using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// どの陣営にも当てはまらない勢力です。
/// </summary>
public sealed class OthersTeam : CustomTeam
{
    public override string Name => "不明な勢力";

    /// <inheritdoc/>
    public override string HudName => "<color=#ffffff>Unknown Forces</color>";

    /// <inheritdoc/>
    public override string Objective => "己の目的を果たせ。";

    public override string CassieName => "Unknown Forces";

    public override string Color => "#ffffff";

    public override bool UsesVanillaEnding => false;

    public override string VictoryText => "<b><size=80><color=#ffffff>UNKNOWN TEAM</color>の勝利</size></b>";

    protected override bool IncludesVanilla(Player player) => false;
}
