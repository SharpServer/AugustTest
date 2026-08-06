using System.Linq;
using AntiMeme.Spawning.Waves;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Spawning;

/// <summary>
/// 核が落ちたあと、一定の確率で Pandra's Box 部隊を送り込みます。
/// </summary>
/// <remarks>
/// 送り込むのは「観戦者が 2 人以上いる」ときだけです。
/// 誰も入れない部隊を出しても意味がないので、人が溜まるまで待ちます。
/// </remarks>
public sealed class PandraBoxDispatch : EventHandlerBase
{
    /// <summary>3 回に 1 回だけ来ます。</summary>
    private const int Chance = 3;

    private const int RequiredSpectators = 2;
    private const float CheckInterval = 1f;
    private const float InitialDelay = 1f;

    private bool queued;

    /// <inheritdoc/>
    public override void OnWarheadDetonated(WarheadDetonatedEventArgs ev) => Queue();

    /// <inheritdoc/>
    public override void OnServerRoundRestarted() => queued = false;

    /// <summary>
    /// 派遣を予約します。核以外 (Omega Warhead など) からも呼べます。
    /// </summary>
    public void Queue()
    {
        if (queued || !Round.IsRoundStarted || Round.IsRoundEnded) return;
        if (Random.Range(0, Chance) != 0) return;

        queued = true;

        RoundScope.Current.Delay(InitialDelay, WaitForSpectators);
    }

    /// <summary>観戦者が揃うまで待ってから送り込みます。</summary>
    private void WaitForSpectators()
    {
        RoundScope.Current.RunLoop(CheckInterval, () =>
        {
            if (!queued) return;

            if (Spectators() < RequiredSpectators) return;

            queued = false;
            SpawnSystem.ForceSpawnNow(new PdxWave());
        });
    }

    /// <summary>
    /// 派遣先になれる観戦者の数です。カスタム役職を持ったままの観戦者は数えません。
    /// </summary>
    private static int Spectators() =>
        Player.ReadyList.Count(player =>
            player is { IsDestroyed: false } &&
            player.Role is RoleTypeId.Spectator &&
            CustomRole.Of(player) is null);
}
