using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// 運営チームです。バニラ役職からは誰も入らず、運営役職が自分で名乗ったときだけ所属します。
/// 勝敗には関わりません。
/// </summary>
public sealed class ModeratorsTeam : CustomTeam
{
    public override string Name => "モデレーター";

    /// <inheritdoc/>
    public override string HudName => "<color=#c0c0c0>Moderators</color>";

    /// <inheritdoc/>
    public override string Objective => "正しくモデレーション処置を行い、秩序を回復し安全なシャープ鯖を死守せよ。";

    public override string Color => "#ff1493";

    public override bool IsGroupOfInterest => false;

    protected override bool IncludesVanilla(Player player) => false;
}
