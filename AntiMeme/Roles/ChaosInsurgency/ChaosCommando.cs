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

public sealed class ChaosCommando : ChaosRole
{
    public override string Name => "カオス・インサージェンシー コマンドー";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#228b22>Chaos Insurgency Commando</color>";
    public override string Description => "カオスの実戦部隊の中でのエリート中のエリート。\n抑圧兵よりも階級は上で、基本的に秩序のない、襲撃部隊を指揮する。";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosRepressor;
    public override float? MaxHealth => 120f;
    public override string CustomInfo => "Chaos Insurgency Commando";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardChaosInsurgency, ItemType.Adrenaline, ItemType.Medkit,
      ItemType.ArmorCombat ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(AdvancedMedkit), typeof(ArmorInfantry), typeof(GunSuperLogicer) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo762x39] = 130 };
}
