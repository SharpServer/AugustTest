using Sliced.API.Structs;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// 警備員です。
/// </summary>
public sealed class GuardsTeam : CustomTeam
{
    public override string Name => "警備員";

    /// <inheritdoc/>
    public override string HudName => "<color=#00b7eb>The Foundation</color>";

    /// <inheritdoc/>
    public override string Objective => "研究員を救出し、施設の秩序を守護せよ。";

    public override string CassieName => "Facility Guard Personnel";

    public override string Color => "#00b7eb";

    public override bool IsGroupOfInterest => false;

    public override IReadOnlyList<CustomTeam> Allies =>
    [
        Get<FoundationForcesTeam>(),
        Get<ScientistsTeam>(),
    ];

    /// <summary>SCP-1509 で蘇生した者はこの陣営の一員として立ちます。</summary>
    public override SpawnSetRoleDefinition? Resurrection => SpawnSetRoleDefinition.Vanilla(RoleTypeId.FacilityGuard);

    protected override bool IncludesVanilla(Player player) => player.Role is RoleTypeId.FacilityGuard;
}
