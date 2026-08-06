using AntiMeme.Items.Bases;
using AntiMeme.Maps;
using AntiMeme.Net;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerRoles;
using UnityEngine;

using ExiledNpc = Exiled.API.Features.Npc;

namespace AntiMeme.Items.Admin;

/// <summary>
/// Dummy を撒く SCP-3005 スキマティック射出器です。投げ捨て操作で発射します。
/// </summary>
public sealed class DummyRoad : CustomKeycard
{
    public override string Name => "Dummy Road Spawner";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.black;

    public override string Description => "What the Fuck?";

    protected override string Label => "Dummy Road Spawn Device";

    protected override string HolderName => "Dummy Lord";

    protected override string SerialNumber => "555555555555";

    protected override Color32 Tint => Color.black;

    protected override Color32 LabelColor => Color.black;

    protected override Color32 PermissionsColor => Color.white;

    protected override int Rank => 1;

    /// <inheritdoc/>
    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw) return;

        ev.IsAllowed = false;
        Destroy();

        new DummyRoadProjectile
        {
            Position = ev.Player.Position,
            Direction = ev.Player.Camera.forward,
            Lifespan = 0.5f,
        }.Create();
    }
}

/// <summary>
/// 前進しながら通り道に NPC を置いていくスキマティックです。
/// <see cref="DummyRoad"/> 専用なので同居させています。
/// </summary>
internal sealed class DummyRoadProjectile : ObjectPrefab
{
    private const float Interval = 0.02f;
    private const float Speed = 20f;

    private int sequence;

    public Vector3 Direction { get; set; }

    protected override string SchematicName => "SCP3005";

    public override bool FollowsMarker => false;

    protected override void OnCreate() => Loop(Interval, Tick);

    private void Tick()
    {
        Position += Direction.normalized * (Interval * Speed);

        ExiledNpc npc = ExiledNpc.Spawn("DummyRoad No. " + sequence++, RoleTypeId.ClassD, Position);

        if (npc is not null)
            InternalNpcs.Register(npc, InternalNpcKind.TeamNpc);
    }
}
