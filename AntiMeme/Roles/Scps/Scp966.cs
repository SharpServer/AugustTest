using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp3114Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Structs;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-966。暗視装置を着けていない相手からは見えず、近づいた部屋の照明を落とします。
///
/// 姿を消す処理はゲーム本体の可視性判定 (<c>PlayerEvents.ValidatedVisibility</c>) に
/// そのまま乗せます。旧実装は 0.1 秒ごとに全プレイヤーを走査して可視リストを作り直し、
/// それを独自の可視性同期レイヤーへ流していましたが、判定はフレーム単位で本体が呼んでくれます。
/// </summary>
public class Scp966 : ScpRole
{
    private const float BlackoutRadius = 15f;
    private const float BlackoutInterval = 0.5f;
    private const float SpeedInterval = 1f;
    private const byte MaxSpeedLevel = 5;
    private const float MaxLevelHeal = 10f;
    private const float AttackDamage = 20f;

    /// <summary>速度レベル 1〜5 に対応する鈍足強度。5 だけは加速に変わります。</summary>
    private static readonly byte[] SlownessByLevel = [30, 20, 10, 0];

    private readonly HashSet<Room> darkenedRooms = [];

    private byte speedLevel = 1;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 9 6 6";

    public override string Name => "SCP-966";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-966</color>";

    /// <inheritdoc/>
    public override string Objective => "不可視のまま獲物を眠らせず、疲弊した人間を狩れ。";

    public override string Description =>
        "透明な眠りを妨げるSCP。\n" +
        "攻撃時敵の足と視界を一時的に妨害することが出来る。";

    public override RoleTypeId BaseRole => RoleTypeId.Scp3114;

    public override float? MaxHealth => 1000f;

    public override string CustomInfo => "SCP-966";

    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.LczGlassroom, Vector3.up * 0.5f);

    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<NightVision>(255),
    ];

    public override Vector3? Scale => new Vector3(0.94f, 1.15f, 0.94f);

    protected override void OnSpawned()
    {
        SetHumeShield(500f);

        Hook(
            () => PlayerEvents.ValidatedVisibility += OnValidatedVisibility,
            () => PlayerEvents.ValidatedVisibility -= OnValidatedVisibility);

        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

        // 3114 の擬態は使わせず、代わりに速度レベルを 1 段上げる操作として転用する。
        Hook(
            () => Scp3114Events.Disguising += OnDisguising,
            () => Scp3114Events.Disguising -= OnDisguising);

        Scope.RunLoop(SpeedInterval, _ => ApplySpeed());
        Scope.RunLoop(BlackoutInterval, _ => UpdateBlackout());

        // 消した照明は役職が終わった時点で必ず戻す。
        Scope.OnDispose(_ => RestoreRooms());
    }

    private void OnValidatedVisibility(PlayerValidatedVisibilityEventArgs ev)
    {
        if (IsMine(ev.Target) && !CanSee(ev.Player))
            ev.IsVisible = false;
    }

    /// <summary>
    /// SCP-966 を視認できる条件です。SCP と死亡者は常に見え、
    /// 生存者は SCP-1344 (暗視ゴーグル) を装着している場合だけ見えます。
    /// </summary>
    private static bool CanSee(Player observer)
    {
        if (observer.IsSCP || !observer.IsAlive) return true;

        return observer.Items.Any(item => item is Scp1344Item { IsWorn: true });
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!IsMine(ev.Attacker) || ev.DamageHandler is not StandardDamageHandler damage) return;

        damage.Damage = AttackDamage + speedLevel;

        ev.Player.EnableEffect<Slowness>(20, 10f);
        ev.Player.EnableEffect<Blindness>(40, 10f);
    }

    private void OnDisguising(Scp3114DisguisingEventArgs ev)
    {
        if (!IsMine(ev.Player)) return;

        ev.IsAllowed = false;
        ev.Ragdoll.Destroy();

        if (speedLevel >= MaxSpeedLevel)
            Player.Heal(MaxLevelHeal);
        else
            speedLevel++;
    }

    private void ApplySpeed()
    {
        if (speedLevel >= MaxSpeedLevel)
            Player.EnableEffect<MovementBoost>(10);
        else
            Player.EnableEffect<Slowness>(SlownessByLevel[speedLevel - 1]);

        ShowStatus($"Speed Level: {speedLevel} / {MaxSpeedLevel}", SpeedInterval + 0.2f);
    }

    /// <summary>
    /// 半径内の部屋を消灯し、離れた部屋は元に戻します。
    /// </summary>
    private void UpdateBlackout()
    {
        HashSet<Room> inRange = Room.List
            .Where(room => (room.Position - Player.Position).sqrMagnitude <= BlackoutRadius * BlackoutRadius)
            .ToHashSet();

        foreach (Room room in inRange)
        {
            if (darkenedRooms.Add(room) && room.LightController is { } lights)
                lights.LightsEnabled = false;
        }

        foreach (Room room in darkenedRooms.ToArray())
        {
            if (inRange.Contains(room)) continue;

            darkenedRooms.Remove(room);

            if (room.LightController is { } lights)
                lights.LightsEnabled = true;
        }
    }

    private void RestoreRooms()
    {
        foreach (Room room in darkenedRooms)
        {
            if (room.LightController is { } lights)
                lights.LightsEnabled = true;
        }

        darkenedRooms.Clear();
    }
}
