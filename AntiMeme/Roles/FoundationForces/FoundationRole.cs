using AntiMeme.Teams.Factions;
using Sliced.API.Features;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// 財団戦力 (機動部隊)の共通土台です。所属を 1 か所で名乗るだけで、振る舞いは持ちません。
/// イベント購読・ステータス表示・ヒュームシールドは <see cref="CustomRole"/> が持っています。
/// </summary>
public abstract class FoundationRole : CustomRole
{
    /// <inheritdoc/>
    public override CustomTeam Team => CustomTeam.Get<FoundationForcesTeam>();

    /// <summary>説明が長めなので既定より表示を伸ばします。</summary>
    protected override float HintDuration => 7f;
}
