using Sliced.API.Structs;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// 科学者です。
/// </summary>
public sealed class ScientistsTeam : CustomTeam
{
    public override string Name => "科学者";

    /// <inheritdoc/>
    public override string HudName => "<color=#faff86>Neutral - Side Foundation</color>";

    /// <inheritdoc/>
    public override string Objective => "施設から脱出せよ";

    public override string CassieName => "Scientist Personnel";

    public override string Color => "#faff86";

    public override bool IsGroupOfInterest => false;

    public override IReadOnlyList<CustomTeam> Allies =>
    [
        Get<FoundationForcesTeam>(),
        Get<GuardsTeam>(),
    ];

    /// <summary>SCP-1509 で蘇生した者はこの陣営の一員として立ちます。</summary>
    public override SpawnSetRoleDefinition? Resurrection => SpawnSetRoleDefinition.Vanilla(RoleTypeId.Scientist);

    protected override bool IncludesVanilla(Player player) => player.Role is RoleTypeId.Scientist;
}
