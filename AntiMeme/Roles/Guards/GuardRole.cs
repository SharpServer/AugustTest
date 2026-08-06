using AntiMeme.Teams.Factions;
using Sliced.API.Features;

namespace AntiMeme.Roles.Guards;

/// <summary>
/// 施設警備員の共通土台です。所属を 1 か所で名乗るだけで、振る舞いは持ちません。
/// イベント購読・ステータス表示・ヒュームシールドは <see cref="CustomRole"/> が持っています。
/// </summary>
public abstract class GuardRole : CustomRole
{
    /// <inheritdoc/>
    public override CustomTeam Team => CustomTeam.Get<GuardsTeam>();
}
