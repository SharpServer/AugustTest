using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Roles.ChaosInsurgency;

public sealed class ChaosSniper : ChaosRole
{
    public override string Name => "カオス・インサージェンシー 狙撃兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#228b22>Chaos Insurgency Sniper</color>";
    public override string Description => "スナイパーライフルを用いて素早く対象を制圧する。";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosRepressor;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Chaos Insurgency Sniper";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardChaosInsurgency, ItemType.Medkit, ItemType.Adrenaline,
      ItemType.ArmorCombat ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(GunSL8) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo556x45] = 100 };
}
