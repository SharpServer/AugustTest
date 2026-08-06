using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// 対象の逃走を防ぐ拘留兵。
/// </summary>
public class NtfDetainer : FoundationRole
{
    public override string Name => "九尾狐 拘留兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>MTF E-11: Detainer</color>";

    public override string Description =>
        "SCiPの行動阻害に特化したNTF特技兵。\n" +
        "XE-11 ANOMALY DETAINERで対象の逃走を防ぐ。";

    public override RoleTypeId BaseRole => RoleTypeId.NtfSergeant;

    public override float? MaxHealth => 100f;

    public override string CustomInfo => "Nine-tailed Fox Detainer";

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunFSP9,
        ItemType.KeycardMTFOperative,
        ItemType.ArmorCombat,
        ItemType.Medkit,
        ItemType.Radio,
        ItemType.Flashlight,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(GunAnomalyDetainer)];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 90,
        [ItemType.Ammo9x19] = 120,
    };
}
