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

public sealed class ChaosUndercoverAgent : ChaosRole
{
    public override string Name => "カオス・インサージェンシー 潜入工作員";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#228b22>Chaos Insurgency Undercover Agent</color>";
    public override string Description => "施設に潜入した先遣隊。施設の偵察や略奪を行え！";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosMarauder;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Chaos Insurgency Undercover Agent";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GrenadeFlash, ItemType.Medkit, ItemType.Adrenaline,
      ItemType.ArmorCombat ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(KeycardConscripts) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo44cal] = 24 };
}
