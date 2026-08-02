using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;

namespace AntiMeme.SpawnSets;

public class FirstRolesSetSCPs : SpawnSet
{
    public override string Id { get; set; } = "FirstRoles.SCPs";
    public override string Name { get; set; } = "First Roles (SCPs)";
    public override string Description { get; set; } = "ラウンド開始時のSCP割り当て。";

    public override int AllowedPlayerCount
    {
        get
        {
            int playerCount = Player.ReadyList.Count();

            return playerCount switch
            {
                < 4 => 1,
                < 8 => 2,
                < 12 => 3,
                _ => 4,
            };
        }

        set
        {
        }
    }

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles { get; set; } =
    [
        new(RoleTypeId.Scp173, null, 1, false),
        new(RoleTypeId.Scp106, null, 1, false),
        new(RoleTypeId.Scp049, null, 1, false),
        new(RoleTypeId.Scp079, null, 1, false),
        new(RoleTypeId.Scp096, null, 1, false),
        new(RoleTypeId.Scp939, null, 1, false),
        new(RoleTypeId.Scp3114, null, 1, false),
    ];
}
public class FirstRolesSetNormals : SpawnSet
{
    public override string Id { get; set; } = "FirstRoles.Normals";
    public override string Name { get; set; } = "First Roles (Normals)";
    public override string Description { get; set; } = "ラウンド開始時の人間側割り当て。";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles { get; set; } =
    [
        new(RoleTypeId.ClassD, null, 1, false),
        new(RoleTypeId.Scientist, null, 1, false),
        new(RoleTypeId.FacilityGuard, null, 1, false),
    ];
}