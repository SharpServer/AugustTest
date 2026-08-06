using AntiMeme.Items.Utility.Snav;
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

public sealed class ChaosSignal : ChaosRole
{
    public override string Name => "カオス・インサージェンシー 通信兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#228b22>Chaos Insurgency Signal</color>";
    public override string Description => "S-Nav 300を用いてユニークな部屋を捜索する。";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosRifleman;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Chaos Insurgency Signal";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardChaosInsurgency, ItemType.Medkit, ItemType.Painkillers,
      ItemType.ArmorCombat, ItemType.GunAK ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo762x39] = 120 };
    public override IReadOnlyList<Type> CustomItems => [ typeof(SNAV300) ];
}
