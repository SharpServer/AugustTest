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

public sealed class ChaosTacticalUnit : ChaosRole
{
    public override string Name => "カオス・インサージェンシー 戦術兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#228b22>Chaos Insurgency Tactical Unit</color>";
    public override string Description => "特殊なリボルバーを用いて邪魔者を排除せよ！";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosMarauder;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Chaos Insurgency Tactical Unit";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardChaosInsurgency, ItemType.Medkit, ItemType.Painkillers,
      ItemType.ArmorCombat, ItemType.GrenadeFlash ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(GunTacticalRevolver) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo44cal] = 40 };
}
