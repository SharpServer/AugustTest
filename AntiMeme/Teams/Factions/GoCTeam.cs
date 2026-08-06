using Sliced.API.Structs;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// 世界オカルト連合です。
/// </summary>
public sealed class GoCTeam : CustomTeam
{
    public override string Name => "世界オカルト連合";

    /// <inheritdoc/>
    public override string HudName => "<color=#0000c8>Global Occult Coalition</color>";

    /// <inheritdoc/>
    public override string Objective => "人類第一に、財団に抵抗せよ。";

    public override string CassieName => "G O C";

    public override string Color => "#0000c8";

    public override VictoryCondition Victory => VictoryCondition.LastStanding(priority: 30);

    public override bool UsesVanillaEnding => false;

    /// <summary>SCP-1509 で蘇生した者はこの陣営の一員として立ちます。</summary>
    public override SpawnSetRoleDefinition? Resurrection => SpawnSetRoleDefinition.Custom<AntiMeme.Roles.GoC.GoCOperative>();

    protected override bool IncludesVanilla(Player player) => false;
}
