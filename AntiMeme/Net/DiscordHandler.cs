using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Enums;
using Sliced.API.Features;

namespace AntiMeme.Net;

/// <summary>
/// Discord へ流す出来事を拾います。
/// <see cref="EventHandlerBase"/> を継承しているだけで Sliced が購読します。
///
/// <para>
/// 旧実装は EXILED のイベントを使い、Kick と Ban が二重に飛ぶのを
/// 「次の Kick を抑制するセット」で捌いていました。LabAPI は
/// <see cref="PlayerEvents.Kicked"/> と <see cref="ServerEvents.BanIssued"/> が
/// 別々に来るので、その打ち消しは要りません。
/// </para>
/// </summary>
public sealed class DiscordHandler : EventHandlerBase
{
    /// <summary>人数を送る間隔 (秒)。</summary>
    private const float PlayerCountInterval = 60f;

    private CoroutineHandle loop;

    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    protected override void OnEnabled()
    {
        if (!Discord.IsConfigured) return;

        // ラウンドをまたいで送り続けるので RoundScope には載せない。
        // このハンドラは Manual なので、破棄されるまで生きる。
        loop = Timing.RunCoroutine(PlayerCountLoop());
    }

    protected override void OnDisposed() => Timing.KillCoroutines(loop);

    private IEnumerator<float> PlayerCountLoop()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(PlayerCountInterval);

            Discord.SendPlayerCount(CountPlayers());
        }
    }

    public override void OnPlayerKicked(PlayerKickedEventArgs ev) => Discord.SendModeration("kick", new
    {
        actor = Describe(ev.Issuer),
        target = ev.Player?.Nickname,
        targetId = ev.Player?.UserId,
        reason = ev.Reason,
    });

    public override void OnServerBanIssued(BanIssuedEventArgs ev) => Discord.SendModeration("ban", new
    {
        banType = ev.BanType.ToString(),
        target = ev.BanDetails.OriginalName,
        targetId = ev.BanDetails.Id,
        issuer = ev.BanDetails.Issuer,
        reason = ev.BanDetails.Reason,
        expires = ev.BanDetails.Expires,
    });

    public override void OnPlayerReportedCheater(PlayerReportedCheaterEventArgs ev) =>
        Discord.SendModeration("report_cheater", new
        {
            actor = Describe(ev.Player),
            target = Describe(ev.Target),
            reason = ev.Reason,
        });

    public override void OnPlayerReportedPlayer(PlayerReportedPlayerEventArgs ev) =>
        Discord.SendModeration("report_local", new
        {
            actor = Describe(ev.Player),
            target = Describe(ev.Target),
            reason = ev.Reason,
        });

    /// <summary>
    /// 人数は実プレイヤーだけ数えます。NPC (タレット・触手・チーム NPC) は除きます。
    /// </summary>
    private static int CountPlayers() =>
        Player.ReadyList.Count(InternalNpcs.IsHuman);

    private static object Describe(Player player) =>
        player is { IsDestroyed: false }
            ? new { name = player.Nickname, id = player.UserId }
            : null;
}
