using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items;
using AntiMeme.Net;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Nvg;

/// <summary>
/// カスタム NVG の視界、ライト、電池を管理します。
/// </summary>
public static class NvgManager
{
    private const float MaxBattery = 100f;
    private const float TickInterval = 0.1f;

    private static readonly Dictionary<ushort, NvgRuntimeData> Active = new();
    private static readonly Dictionary<ushort, float> Batteries = new();
    private static readonly Dictionary<ushort, CoroutineHandle> Watches = new();
    private static bool hooked;

    internal static void Hook()
    {
        if (hooked)
            return;

        hooked = true;
        PlayerEvents.DetectedByScp1344 += OnDetectedByScp1344;
        PlayerEvents.Left += OnLeft;
        ServerEvents.RoundRestarted += Clear;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.DetectedByScp1344 -= OnDetectedByScp1344;
            PlayerEvents.Left -= OnLeft;
            ServerEvents.RoundRestarted -= Clear;
            Clear();
            hooked = false;
        });
    }

    /// <summary>
    /// 掛かり切った時点で点けます。<c>CustomGoggles.OnWorn</c> から呼ばれます。
    /// </summary>
    /// <remarks>
    /// 旧版はここで 0.1 秒ごとの監視ループを張り、装着状態の判定と暗転の書き戻しを
    /// 毎 tick 行っていました。それが <c>Scp1344Item.ServerUpdateActive</c> の
    /// 暗転解除と噛み合わず、装着演出が終わらない原因になっていました。
    /// 掛け外しの判定は <c>CustomGoggles</c> が状態遷移から拾うので、
    /// ここに残すのは電池とライトの更新だけです。
    /// </remarks>
    internal static void Start(ushort serial, Player player, NvgProfile profile)
    {
        Hook();
        StopWatch(serial);
        Begin(player, serial, profile);

        if (!Active.ContainsKey(serial)) return;

        Watches[serial] = PlayerScope.Of(player).RunLoop(TickInterval, _ =>
        {
            if (!Active.TryGetValue(serial, out NvgRuntimeData data))
            {
                StopWatch(serial);

                return;
            }

            Tick(data);
        });
    }

    internal static void Stop(ushort serial)
    {
        if (serial == 0)
            return;

        StopWatch(serial);
        StopActive(serial);
    }

    private static void StopActive(ushort serial)
    {
        if (!Active.TryGetValue(serial, out NvgRuntimeData data))
            return;

        Active.Remove(serial);
        data.Light?.Base?.netIdentity.RemoveShowState();
        data.Light?.Destroy();

        // 電池切れで自分が塞いだぶんだけ戻す。通常の取り外し演出はゲーム側が面倒を見るので、
        // ここで Intensity = 0 にすると外し途中の暗転まで消してしまう。
        if (data.Profile.UseBlackout && TryGetItem(serial, out Scp1344Item item) &&
            item.BlindnessEffect.Intensity == byte.MaxValue)
        {
            item.BlindnessEffect.Intensity = data.Profile.WornBlindnessIntensity;
        }
    }

    public static bool IsActive(ushort serial) => Active.ContainsKey(serial);

    public static bool HasBattery(ushort serial) =>
        Batteries.TryGetValue(serial, out float battery) && battery > 0f;

    public static float GetBattery(ushort serial, float fallback = 0f) =>
        Batteries.TryGetValue(serial, out float battery) ? battery : fallback;

    public static bool TrySetBattery(ushort serial, float battery, bool reviveIfDead = false)
    {
        battery = Mathf.Clamp(battery, 0f, MaxBattery);
        Batteries[serial] = battery;

        if (!Active.TryGetValue(serial, out NvgRuntimeData data))
            return true;

        UpdateLight(data, battery);
        if (reviveIfDead && battery > 0f && TryGetItem(serial, out Scp1344Item item))
            ApplyBlindness(data, item, battery);

        return true;
    }

    public static bool AddBattery(ushort serial, float amount) =>
        TrySetBattery(serial, GetBattery(serial, MaxBattery) + amount, true);

    public static bool DrainBattery(ushort serial, float amount) =>
        TrySetBattery(serial, GetBattery(serial) - amount);

    public static bool TryGetPlayerNvgItem(Player player, ushort serial, out Scp1344Item item)
    {
        item = player?.Items.OfType<Scp1344Item>().FirstOrDefault(candidate => candidate.Serial == serial);
        return item is not null;
    }

    private static void Begin(Player player, ushort serial, NvgProfile profile)
    {
        StopActive(serial);

        if (!TryGetPlayerNvgItem(player, serial, out Scp1344Item item))
            return;

        // 幻覚効果の除去と顔の見た目は CustomGoggles が掛け外しの両方で面倒を見る。
        // ここで重ねて掛けると、外すときに片方だけ残る。

        LightSourceToy light = LightSourceToy.Create(player.Camera.position, player.Rotation, player.Camera, false);
        if (light is null)
            return;

        light.Color = profile.LightColor;
        light.Range = profile.LightRange;
        light.Intensity = profile.LightIntensity;
        light.Spawn();

        NetworkIdentity identity = light.Base.netIdentity;
        identity?.InitShowState(new NetworkShowState { Owner = player, ShowToOwner = true, ShowToSpectators = true });

        float battery = profile.DrainPerSecond <= 0f ? MaxBattery : GetBattery(serial, MaxBattery);
        NvgRuntimeData data = new(player, serial, profile, light);
        Active[serial] = data;
        Batteries[serial] = battery;
        ApplyBlindness(data, item, battery);
        UpdateLight(data, battery);
    }

    private static void Tick(NvgRuntimeData data)
    {
        if (data.Owner is not { IsDestroyed: false, IsAlive: true } ||
            !TryGetPlayerNvgItem(data.Owner, data.Serial, out Scp1344Item item) || !item.IsWorn)
        {
            Stop(data.Serial);
            return;
        }

        float battery = data.Profile.DrainPerSecond <= 0f
            ? MaxBattery
            : Mathf.Max(0f, GetBattery(data.Serial));
        if (data.Profile.DrainPerSecond > 0f)
        {
            battery = Mathf.Max(0f, battery - data.Profile.DrainPerSecond * TickInterval);
            Batteries[data.Serial] = battery;
        }

        UpdateLight(data, battery);
        ApplyBlindness(data, item, battery);
    }

    /// <summary>
    /// 電池切れの暗転だけを掛けます。
    /// </summary>
    /// <remarks>
    /// <b>電池が残っている間は Intensity に触りません。</b>
    /// <c>Scp1344Item.ServerUpdateActive</c> が装着直後の暗転を毎フレーム下げていくので、
    /// ここから書き戻すとその処理が「もう晴れた」と判定できず、視界が塞がったままになります。
    /// </remarks>
    private static void ApplyBlindness(NvgRuntimeData data, Scp1344Item item, float battery)
    {
        if (!data.Profile.UseBlackout) return;

        if (battery <= 0f)
            item.BlindnessEffect.Intensity = byte.MaxValue;
        else if (item.BlindnessEffect.Intensity == byte.MaxValue)
            item.BlindnessEffect.Intensity = data.Profile.WornBlindnessIntensity;
    }

    private static void UpdateLight(NvgRuntimeData data, float battery)
    {
        if (data.Light is null)
            return;

        if (data.Profile.DrainPerSecond <= 0f || battery > 0f)
        {
            float ratio = data.Profile.DrainPerSecond <= 0f ? 1f : battery / MaxBattery;
            data.Light.Range = data.Profile.LightRange;
            data.Light.Intensity = data.Profile.LightIntensity * (0.4f + 0.6f * ratio);
        }
        else
        {
            data.Light.Range = 0f;
            data.Light.Intensity = 0f;
        }
    }

    private static void StopWatch(ushort serial)
    {
        if (Watches.Remove(serial, out CoroutineHandle handle) && handle.IsRunning)
            Timing.KillCoroutines(handle);
    }

    private static bool TryGetItem(ushort serial, out Scp1344Item item)
    {
        item = Item.Get(serial) as Scp1344Item;
        return item is not null;
    }

    private static void OnDetectedByScp1344(PlayerDetectedByScp1344EventArgs ev)
    {
        // LabAPI's event is emitted for each x-ray target. Reapply the managed visual state
        // after the vanilla provider updates the effect so battery blackout cannot be lost.
        if (ev.Player?.CurrentItem is not Scp1344Item item || !Active.TryGetValue(item.Serial, out NvgRuntimeData data))
            return;

        ApplyBlindness(data, item, GetBattery(item.Serial, MaxBattery));
    }

    private static void OnLeft(PlayerLeftEventArgs ev)
    {
        foreach (NvgRuntimeData data in Active.Values.Where(data => data.Owner == ev.Player).ToList())
            Stop(data.Serial);
    }

    private static void Clear()
    {
        foreach (ushort serial in Active.Keys.ToList())
            Stop(serial);
        foreach (ushort serial in Watches.Keys.ToList())
            StopWatch(serial);
        Active.Clear();
        Watches.Clear();
        Batteries.Clear();
    }

    private sealed class NvgRuntimeData
    {
        public NvgRuntimeData(Player owner, ushort serial, NvgProfile profile, LightSourceToy light)
        {
            Owner = owner;
            Serial = serial;
            Profile = profile;
            Light = light;
        }

        public Player Owner { get; }
        public ushort Serial { get; }
        public NvgProfile Profile { get; }
        public LightSourceToy Light { get; }
    }
}
