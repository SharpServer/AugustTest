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

public sealed class ChaosPenal : ChaosRole
{
    public override string Name => "カオス・インサージェンシー 懲罰兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#228b22>Chaos Insurgency Breaker</color>";
    public override string Description => "貴方は問題を起こしてここに連れてこられてしまった哀れなカオスだ。\nとにかく大量に爆発物を持たされている。特攻しろ！";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosConscript;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Chaos Insurgency Penal";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.ArmorCombat, ItemType.GrenadeHE, ItemType.GrenadeHE,
      ItemType.GrenadeHE, ItemType.SCP207, ItemType.AntiSCP207 ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(KeycardConscripts) ];
}
