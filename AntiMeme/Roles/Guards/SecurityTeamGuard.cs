using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Guards;

public sealed class SecurityTeamGuard : GuardRole
{
    public override string Name => "保安部隊員";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>Security Team Guard</color>";
    public override string Description => "職員たちを保護し、脱出を助ける。";
    public override RoleTypeId BaseRole => RoleTypeId.FacilityGuard;
    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Security Team Guard";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardGuard, ItemType.Medkit, ItemType.Painkillers,
      ItemType.ArmorCombat, ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(GunFSP18) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 110 };
}
