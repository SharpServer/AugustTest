using System;
using System.Linq;
using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

using ExiledRound = Exiled.API.Features.Round;

namespace AntiMeme.Changes;

/// <summary>
/// 開幕直後に SCP が抜けて SCP が居なくなったとき、観戦者を 1 人その役職で立て直します。
/// </summary>
/// <remarks>
/// <para>
/// 抜けた本人の役職 (カスタムならカスタムのまま) を引き継がせます。
/// SCP-049-2 のような「増えた側」は元から代役を立てません。
/// </para>
/// <para>
/// 猶予は開始から <see cref="GraceSeconds"/> 秒までです。それを過ぎた離脱は
/// ラウンドの結果として扱い、埋め合わせません。
/// </para>
/// </remarks>
public sealed class ScpDisconnectRelief : EventHandlerBase
{
    /// <summary>代役を立てる猶予です。</summary>
    private const double GraceSeconds = 179d;

    /// <inheritdoc/>
    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        if (ev.Player is not { } leaving) return;
        if (!ExiledRound.InProgress || ExiledRound.ElapsedTime.TotalSeconds > GraceSeconds) return;

        // 抜けたのが SCP でなければ何もしない。
        if (CustomTeam.Of(leaving) is not ScpTeam) return;

        // 抜けた本人の役職はここで写し取る。
        // CustomRole も Left を購読して登録を落とすので、後から引き直すと消えている。
        // (購読はプラグイン起動時のこちらが先だが、取れなかった場合はバニラ役職で代替する)
        RoleTypeId vanilla = leaving.Role;
        Type custom = CustomRole.Of(leaving)?.GetType();

        // 増えた側 (SCP-049-2 / ゾンビ) は元々 1 枠ではないので補充しない。

        if (vanilla is RoleTypeId.Scp0492) return;

        // まだ他に SCP が生きているなら埋め合わせは要らない。
        bool anyScpLeft = Player.ReadyList.Any(player =>
            player is { IsDestroyed: false, IsAlive: true } &&
            !ReferenceEquals(player, leaving) &&
            CustomTeam.Of(player) is ScpTeam);

        if (anyScpLeft) return;

        if (Player.ReadyList.FirstOrDefault(player =>
                player is { IsDestroyed: false, IsPlayer: true, IsAlive: false } &&
                !ReferenceEquals(player, leaving)) is not { } replacement)
        {
            return;
        }

        if (custom is not null)
            CustomRole.Spawn(custom, replacement);
        else
            replacement.SetRole(vanilla);

        replacement.SendHint("※SCPプレイヤーが切断したため代わりにスポーンしました", 5f);
    }
}
