using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Features;

using ExiledRound = Exiled.API.Features.Round;

namespace AntiMeme.Hud;

/// <summary>
/// pull スロットを回す唯一のループです。<see cref="HudSlot.Interval"/> ごとに
/// <see cref="HudSlot.Source"/> を呼んで全員の HUD へ書き戻します。
///
/// 旧実装ではドメインごとに 4 本のコルーチンが独立して回っていて、
/// それぞれが「ヒントが無ければ作り直す」処理を抱えていました。ここでは
/// 生成は <see cref="PlayerHud.Set"/> に 1 本化し、寿命は
/// <see cref="RoundScope"/> に預けます。
///
/// NPC を弾く判定はここでは行いません。ヒント配信の最終防衛線は
/// HintServiceMeow 側であって、HUD ループではないためです。
/// </summary>
public sealed class HudLoop : EventHandlerBase
{
    private static RoundScope armedScope;
    private readonly List<CoroutineHandle> loops = [];

    /// <summary>
    /// 常駐します。ラウンドごとのループ張り直しは
    /// <see cref="OnServerWaitingForPlayers"/> で行います。
    /// </summary>
    public override void RegisterEvents()
    {
        HudRenderer.Initialize();
        Arm();
    }

    /// <inheritdoc/>
    public override void OnServerWaitingForPlayers() => Arm();

    /// <inheritdoc/>
    public override void OnPlayerJoined(PlayerJoinedEventArgs ev) => RefreshAll(ev.Player);

    /// <inheritdoc/>
    public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev) => RefreshAll(ev.Player);

    /// <inheritdoc/>
    public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev) => RefreshAll(ev.Player);

    /// <summary>
    /// 観戦先が変わったら、その場で観戦者の画面を作り直します。
    /// </summary>
    /// <remarks>
    /// これが無いと、切り替えた直後は前の観戦先の内容が最大
    /// <see cref="HudSlot.Interval"/> 秒ぶん残ります (役職・陣営・目的は 1 秒)。
    /// <c>ChangedSpectator</c> は事後イベントなので、この時点で
    /// <c>Player.CurrentlySpectating</c> は新しい相手を指しています。
    /// </remarks>
    public override void OnPlayerChangedSpectator(PlayerChangedSpectatorEventArgs ev) => RefreshAll(ev.Player);

    /// <inheritdoc/>
    public override void OnScp079Pinged(Scp079PingedEventArgs ev) => HudRenderer.ShowScp079Ping(ev);

    /// <inheritdoc/>
    public override void OnServerRoundRestarted()
    {
        StopLoops();
        BossBar.HideAll();
        PlayerHud.ClearEveryone();
    }

    /// <inheritdoc/>
    protected override void OnDisabled()
    {
        StopLoops();
        BossBar.HideAll();
        PlayerHud.ClearEveryone();
    }

    /// <summary>
    /// 1 人分の pull スロットをその場で全部更新します。
    /// スポーン直後など、次のループを待たずに見せたいときに使います。
    /// </summary>
    public static void RefreshAll(Player player)
    {
        PlayerHud hud = player.Hud();

        foreach (HudSlot slot in HudSlot.All)
        {
            if (!IsDrawable(slot)) continue;

            hud.Refresh(slot);
        }
    }

    /// <summary>
    /// ロビー中に役職系のスロットを描かないための判定です。
    /// ロビーでは全員 Tutorial なので、そのまま描くと
    /// 「Role: Tutorial / Team: 中立 / Objective: 生き残れ。」が並びます。
    /// </summary>
    private static bool IsDrawable(HudSlot slot) => slot.ShowsInLobby || !ExiledRound.IsLobby;

    /// <summary>
    /// 現在のラウンドスコープにループを張ります。同じスコープへ二重に張りません。
    /// </summary>
    private void Arm()
    {
        RoundScope scope = RoundScope.Current;

        if (ReferenceEquals(armedScope, scope)) return;

        StopLoops();
        armedScope = scope;

        foreach (HudSlot slot in HudSlot.All)
        {
            if (slot.Interval <= 0f) continue;

            HudSlot target = slot;
            loops.Add(scope.RunLoop(target.Interval, () => Tick(target)));
        }
    }

    private void StopLoops()
    {
        foreach (CoroutineHandle loop in loops)
        {
            Timing.KillCoroutines(loop);
        }

        loops.Clear();
        armedScope = null;
    }

    private static void Tick(HudSlot slot)
    {
        if (slot.Source is null) return;

        bool drawable = IsDrawable(slot);

        foreach (Player player in Player.ReadyList)
        {
            PlayerHud hud = new PlayerHud(player);

            // ロビー中は空にする。作らずに黙って飛ばすと、直前のラウンドの文字が残る。
            hud.Set(slot, drawable ? slot.Source(player, hud.Subject) : string.Empty);
        }
    }
}
