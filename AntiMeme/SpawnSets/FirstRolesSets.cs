using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;

namespace AntiMeme.SpawnSets;

public class FirstRolesSetSCPs : SpawnSet
{
    public override string Id { get; set; }
    public override string Name { get; set; }
    public override string Description { get; set; }

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
    public override string Id { get; set; }
    public override string Name { get; set; }
    public override string Description { get; set; }

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles { get; set; } =
    [
        new(RoleTypeId.ClassD, null, 1, false),
        new(RoleTypeId.Scientist, null, 1, false),
        new(RoleTypeId.FacilityGuard, null, 1, false),
    ];
}