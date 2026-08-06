using Sliced.API.Structs;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// カオス・インサージェンシーです。
/// </summary>
public sealed class ChaosInsurgencyTeam : CustomTeam
{
    public override string Name => "カオス・インサージェンシー";

    /// <inheritdoc/>
    public override string HudName => "<color=#228b22>Chaos Insurgency</color>";

    /// <inheritdoc/>
    public override string Objective => "Dクラス職員を救出し、施設を略奪せよ。";

    public override string CassieName => "Chaos Insurgency";

    public override string Color => "#228b22";

    /// <summary>SCP-1509 で蘇生した者はこの陣営の一員として立ちます。</summary>
    public override SpawnSetRoleDefinition? Resurrection => SpawnSetRoleDefinition.Vanilla(RoleTypeId.ChaosConscript);

    protected override bool IncludesVanilla(Player player) => player.IsChaos;
}
