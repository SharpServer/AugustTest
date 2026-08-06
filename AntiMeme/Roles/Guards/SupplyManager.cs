using AntiMeme.Roles;
using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Guards;

public sealed class SupplyManager : GuardRole
{
    public override string Name => "施設供給管理官";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>Supply Manager</color>";

    /// <inheritdoc/>
    public override string Objective => "施設内に向かい警備員たちと合流し、備品と搬入口の管理を遂行せよ。";
    public override string Description => "施設の備品等の搬出入などを管理している職員。\n施設内に向かい警備員たちと合流せよ。";
    public override RoleTypeId BaseRole => RoleTypeId.FacilityGuard;

    /// <summary>マップ側のマーカーで指定します。マーカーが無ければバニラの地点です。</summary>
    public override Vector3? SpawnPosition => SpawnPoints.Tagged("SupplyManagerSpawnPoint");
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Supply Manager";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.Medkit, ItemType.Medkit, ItemType.ArmorLight, ItemType.Radio,
      ItemType.GunCOM18 ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(KeycardSupplyManager) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 80 };
}
