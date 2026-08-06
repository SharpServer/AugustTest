using System.Collections.Generic;
using System.Linq;
using AntiMeme.Roles.Scps;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;

namespace AntiMeme.Spawning;

/// ラウンド開始時の人間側割り当てです。
/// SCP 枠が埋まった後の全員が対象になります。
/// </summary>
public sealed class FirstRolesHumans : SpawnSet
{
    public override string Name => "First Roles (Humans)";

    public override string Description => "ラウンド開始時の人間側割り当て。";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.ClassD, count: 99, weight: 4f),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.Scientist, count: 99, weight: 2f),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.FacilityGuard, count: 99, weight: 3f),
    ];
}
