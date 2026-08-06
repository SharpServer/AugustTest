using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Enums;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Teams;

/// <summary>
/// 独自陣営の勝利を判定してラウンドを終わらせます。
/// <see cref="EventHandlerBase"/> を継承しているだけで Sliced が購読します。
///
/// <para>
/// 判定を回す場所は 1 つだけです。ゲーム側が「もう終わってよいか」を訊いてくる
/// <see cref="ServerEvents.RoundEndingConditionsCheck"/> に乗るので、自前のタイマーは要りません。
/// </para>
/// <para>
/// 独自の勝ち方をする陣営 (<see cref="CustomTeam.UsesVanillaEnding"/> が false) が
/// 生き残っている間は、バニラに勝手に終わらせません。旧実装の
/// <c>requiresVanillaEndLock</c> がこれにあたります。
/// </para>
/// </summary>
public class RoundEndHandler : EventHandlerBase
{
    /// <summary>
    /// 勝利表示を出してから実際にラウンドを終わらせるまでの秒数です。
    /// </summary>
    private const float EndDelay = 10f;

    private bool ended;

    public override HandlerLifetime Lifetime => HandlerLifetime.Round;

    public override void OnServerRoundEndingConditionsCheck(RoundEndingConditionsCheckEventArgs ev)
    {
        if (ended)
        {
            ev.CanEnd = false;

            return;
        }

        if (CustomTeam.FindWinner() is { } winner)
        {
            // バニラの終了処理で正しく表示できる陣営は、そのまま任せる。
            if (winner.UsesVanillaEnding) return;

            ev.CanEnd = false;
            End(winner);

            return;
        }

        // 独自終了の陣営がまだ生きているなら、バニラに終わらせない。
        if (HasCustomEndingTeamAlive())
            ev.CanEnd = false;
    }

    private void End(CustomTeam winner)
    {
        ended = true;

        Logger.Debug($"[AntiMeme] {winner.Name} の勝利でラウンドを終了します。");

        foreach (Player player in Player.ReadyList)
        {
            player.SendHint(winner.VictoryText, EndDelay);
        }

        // ラウンド終了は RoundScope に載せる。途中でラウンドが落ちても置き去りにならない。
        RoundScope.Current.Delay(EndDelay, () => Round.End(force: true));
    }

    private static bool HasCustomEndingTeamAlive()
    {
        return CustomTeam.All.Any(team =>
            team is { UsesVanillaEnding: false } faction &&
            faction.Members.Any());
    }
}
