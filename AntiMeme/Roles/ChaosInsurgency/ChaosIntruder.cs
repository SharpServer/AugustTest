using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.ChaosInsurgency;

public sealed class ChaosIntruder : ChaosRole
{
    public override string Name => "カオス・インサージェンシー 侵入兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#228b22>Chaos Insurgency Intruder</color>";
    public override string Description => "施設に侵入した小規模部隊。警備隊の壊滅及び仲間の脱出を目指せ！";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosMarauder;
    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Chaos Insurgency Intruder";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GrenadeFlash, ItemType.Medkit, ItemType.Adrenaline,
      ItemType.ArmorCombat ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(GunSuppressiver), typeof(KeycardChaosIntruder) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 100 };
}
