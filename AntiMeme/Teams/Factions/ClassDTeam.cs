using Sliced.API.Structs;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// D クラス職員です。
/// </summary>
public sealed class ClassDTeam : CustomTeam
{
    public override string Name => "Dクラス職員";

    /// <inheritdoc/>
    public override string HudName => "<color=#ee7600>Neutral - Side Chaos</color>";

    /// <inheritdoc/>
    public override string Objective => "施設から脱出せよ";

    public override string CassieName => "Class D Personnel";

    public override string Color => "#ee7600";

    /// <summary>SCP-1509 で蘇生した者はこの陣営の一員として立ちます。</summary>
    public override SpawnSetRoleDefinition? Resurrection => SpawnSetRoleDefinition.Vanilla(RoleTypeId.ClassD);

    protected override bool IncludesVanilla(Player player) => player.Role is RoleTypeId.ClassD;
}
