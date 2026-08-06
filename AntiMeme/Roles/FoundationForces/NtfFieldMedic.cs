using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// 遠距離から味方を治療する野戦衛生兵。
/// </summary>
public class NtfFieldMedic : FoundationRole
{
    public override string Name => "NTF FIELD MEDIC";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>MTF E-11: Field Medic</color>";

    public override string Description =>
        "Nine-Tailed-Foxの野戦衛生兵。\n" +
        "S-41 MEDICAL PISTOLで遠距離から味方を治療できる。\n" +
        "敵勢力も回復してしまうため射線に注意。";

    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;

    public override float? MaxHealth => 100f;

    public override string CustomInfo => "Nine-tailed Fox Field Medic";

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunCrossvec,
        ItemType.KeycardMTFOperative,
        ItemType.ArmorCombat,
        ItemType.Radio,
        ItemType.Flashlight,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(S41MedicalPistol), typeof(AdvancedMedkit)];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo9x19] = 120,
    };
}
