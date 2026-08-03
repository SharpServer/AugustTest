using Exiled.API.Enums;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;
using Room = Exiled.API.Features.Room;

namespace AntiMeme.CustomRoles.SCPs;

public class Scp3005 : CustomRole
{
    public override string Name => "SCP-3005";
    public override string Description => "非常に第五的な光！！！！！";
    public override RoleTypeId SpawnRoleType { get; protected set; } = RoleTypeId.Scp0492;
    public override Vector3 SpawnPosition { get; protected set; } = Room.Get(RoomType.Hcz939)?.WorldPosition(Vector3.zero) + Vector3.up ?? Vector3.zero;

    protected override void OnSpawned()
    {
        
    }
}