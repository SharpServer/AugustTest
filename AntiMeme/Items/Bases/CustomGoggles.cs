using System;
using System.Collections.Generic;
using AntiMeme.Items;
using InventorySystem.Items.Usables.Scp1344;
using LabApi.Features.Wrappers;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;
using Sliced.API.Features;

using CustomPlayerEffects;
using ExiledPlayer = Exiled.API.Features.Player;
using Scp1344Handlers = Exiled.Events.Handlers.Scp1344;
using ChangedStatusEventArgs = Exiled.Events.EventArgs.Scp1344.ChangedStatusEventArgs;
using ChangingStatusEventArgs = Exiled.Events.EventArgs.Scp1344.ChangingStatusEventArgs;
using DeactivatingEventArgs = Exiled.Events.EventArgs.Scp1344.DeactivatingEventArgs;

namespace AntiMeme.Items.Bases;

/// <summary>
/// SCP-1344 を土台にした「掛け外しできるゴーグル」の共通土台です。
/// </summary>
/// <remarks>
/// <para>
/// SCP-1344 は <b>自分で状態機械を回しています</b>
/// (<c>Equipping</c> 1.3 秒 → <c>Activating</c> 5 秒 → <c>Stabbing</c> → <c>Active</c>)。
/// <c>Active</c> に入ってからも <c>Scp1344Item.ServerUpdateActive</c> が
/// <c>BlindnessEffect.Intensity</c> を毎フレーム下げて暗転を晴らします。
/// </para>
/// <para>
/// <b>この流れに横から書き込んではいけません。</b> 実際、旧実装をポーリングに置き換えた版は
/// 0.1 秒ごとに <c>BlindnessEffect.Intensity</c> を上書きしていたため暗転が終わらず、
/// さらに <c>EnableWearables</c> した <c>Scp1344Goggles</c> を外す経路が無かったので
/// 外した後もゴーグルが顔に残っていました。
/// </para>
/// <para>
/// ここでは状態の遷移に相乗りするだけにします。掛かる / 外れる時間を変えたいときは
/// ゲーム側が数える <c>_useTime</c> に下駄を履かせ、演出そのものはゲームに任せます。
/// </para>
/// </remarks>
public abstract class CustomGoggles : CustomItem
{
    /// <summary>ゲーム本体が装着に使う秒数です。</summary>
    private const float VanillaWearSeconds = 5f;

    /// <summary>ゲーム本体が取り外しに使う秒数です。</summary>
    private const float VanillaRemoveSeconds = 5.1f;

    private static readonly HashSet<ushort> Registered = new HashSet<ushort>();
    private static bool hooked;

    protected CustomGoggles() => Hook();

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SCP1344;

    /// <summary>装着に掛かる秒数です。既定はゲーム本体と同じ 5 秒。</summary>
    protected virtual float WearingTime => VanillaWearSeconds;

    /// <summary>取り外しに掛かる秒数です。既定はゲーム本体と同じ 5.1 秒。</summary>
    protected virtual float RemovingTime => VanillaRemoveSeconds;

    /// <summary>
    /// SCP-1344 本来の幻覚効果を消すかどうか。
    /// false にすると、ただの暗視ゴーグルではなく SCP-1344 そのものとして振る舞います。
    /// </summary>
    protected virtual bool Remove1344Effect => true;

    /// <summary>
    /// 目を潰さずに外せるかどうか。false ならゲーム本体どおり失明します。
    /// </summary>
    protected virtual bool CanBeRemovedSafely => true;

    /// <summary>掛け終わったときに呼ばれます。</summary>
    protected virtual void OnWorn(Player player)
    {
    }

    /// <summary>外し終わったときに呼ばれます。</summary>
    protected virtual void OnRemoved(Player player)
    {
    }

    /// <summary>いま掛かっているかどうか。</summary>
    public bool IsWorn => Item is LabApi.Features.Wrappers.Scp1344Item { IsWorn: true };

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;

        Scp1344Handlers.ChangedStatus += OnAnyChangedStatus;
        Scp1344Handlers.ChangingStatus += OnAnyChangingStatus;
        Scp1344Handlers.Deactivating += OnAnyDeactivating;

        ItemRuntime.Register(() =>
        {
            Scp1344Handlers.ChangedStatus -= OnAnyChangedStatus;
            Scp1344Handlers.ChangingStatus -= OnAnyChangingStatus;
            Scp1344Handlers.Deactivating -= OnAnyDeactivating;
            Registered.Clear();
            hooked = false;
        });
    }

    private static CustomGoggles Resolve(ushort serial) => Of(serial) as CustomGoggles;

    private static void OnAnyChangedStatus(ChangedStatusEventArgs ev)
    {
        if (ev?.Scp1344 is not { } scp1344 || Resolve(scp1344.Serial) is not { } goggles) return;

        switch (ev.Scp1344Status)
        {
            // 演出の秒数はゲームが _useTime で数えている。開始時に下駄を履かせて長さだけ変える。
            case Scp1344Status.Activating:
                scp1344.Base._useTime = VanillaWearSeconds - goggles.WearingTime;
                break;

            case Scp1344Status.Active:
                goggles.Wear(ev.Player);
                break;

            case Scp1344Status.Deactivating:
                scp1344.Base._useTime = VanillaRemoveSeconds - goggles.RemovingTime;
                break;
        }
    }

    private static void OnAnyChangingStatus(ChangingStatusEventArgs ev)
    {
        if (ev is not { IsAllowed: true } || ev.Scp1344 is not { } scp1344) return;
        if (ev.Scp1344StatusOld != Scp1344Status.Deactivating || ev.Scp1344StatusNew != Scp1344Status.Idle) return;
        if (Resolve(scp1344.Serial) is not { } goggles) return;

        goggles.Remove(ev.Player);
    }

    /// <summary>
    /// 安全に外せるゴーグルは、ゲーム本体の「目を潰す」遷移へ行かせず Idle へ返します。
    /// </summary>
    private static void OnAnyDeactivating(DeactivatingEventArgs ev)
    {
        if (ev is not { IsAllowed: true } || ev.Scp1344 is not { } scp1344) return;
        if (Resolve(scp1344.Serial) is not { CanBeRemovedSafely: true }) return;

        ev.NewStatus = Scp1344Status.Idle;
        ev.IsAllowed = false;
    }

    private void Wear(ExiledPlayer exiled)
    {
        if (Player.Get(exiled?.ReferenceHub) is not { IsDestroyed: false } player) return;

        if (Remove1344Effect)
        {
            // 幻覚は消し、顔のゴーグルだけ残す。外すときに必ず DisableWearables で戻す。
            player.DisableEffect<Scp1344>();
            player.ReferenceHub.EnableWearables(WearableElements.Scp1344Goggles);
            Registered.Add(Serial);
        }

        Invoke(() => OnWorn(player), nameof(OnWorn));
    }

    private void Remove(ExiledPlayer exiled)
    {
        if (Player.Get(exiled?.ReferenceHub) is not { IsDestroyed: false } player) return;

        if (!Remove1344Effect)
            player.DisableEffect<Scp1344>();

        if (CanBeRemovedSafely)
        {
            player.DisableEffect<Blindness>();

            if (player.ReferenceHub is { } hub)
                hub.DisableWearables(WearableElements.Scp1344Goggles);
        }

        Registered.Remove(Serial);

        Invoke(() => OnRemoved(player), nameof(OnRemoved));
    }

    /// <summary>
    /// 掛けたまま手放した (死亡・ロール変更・アイテム破棄) 場合の後始末です。
    /// 顔のゴーグルを消し損ねると、次のラウンドまで見た目が残ります。
    /// </summary>
    protected override void OnReleased()
    {
        if (!Registered.Remove(Serial)) return;

        if (Owner is { IsDestroyed: false } owner && owner.ReferenceHub is { } hub)
            hub.DisableWearables(WearableElements.Scp1344Goggles);
    }

    /// <summary>例外で状態機械を巻き込まないよう、派生の処理だけ包んで呼びます。</summary>
    private void Invoke(Action action, string name)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            LabApi.Features.Console.Logger.Error($"[{GetType().Name}] {name} で例外が発生しました: {exception}");
        }
    }
}
