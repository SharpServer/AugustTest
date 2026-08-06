using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// Epsilon-11 の司令官。
/// </summary>
public class NtfGeneral : FoundationRole
{
    public override string Name => "九尾狐 司令官";

    /// <inheritdoc/>
    public override string HudLabel => "<color=blue>MTF E-11: General</color>";

    public override string Description => "Epsilon-11を率いる高位の司令官。\n隊長等と連携し、確実に施設に安定をもたらせ！";

    public override RoleTypeId BaseRole => RoleTypeId.NtfCaptain;

    public override float? MaxHealth => 100f;

    public override string CustomInfo => "Nine-tailed Fox General";

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFCaptain,
        ItemType.GrenadeHE,
        ItemType.GrenadeHE,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems =>
    [
        typeof(SerumD),
        typeof(AdvancedMedkit),
        typeof(ArmorVip),
        typeof(GunFRMGX),
    ];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 320,
    };
}
