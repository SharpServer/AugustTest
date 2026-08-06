using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// 境界線イニシアチブです。
/// </summary>
public sealed class InitiativeTeam : CustomTeam
{
    public override string Name => "境界線イニシアチブ";

    /// <inheritdoc/>
    public override string HudName => "<color=#4DFFB8>Horizon Initiative</color>";

    /// <inheritdoc/>
    public override string Objective => "異なる神に属する奴らを一人残らず絶滅させろ";

    public override string CassieName => "X Power Forces";

    public override string Color => "#4DFFB8";

    public override VictoryCondition Victory => VictoryCondition.LastStanding(priority: 10);

    public override bool UsesVanillaEnding => false;

    protected override bool IncludesVanilla(Player player) => false;
}
