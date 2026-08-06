using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// 隊長補佐。隊長が倒れたら代理を務めます。
/// </summary>
public class NtfLieutenant : FoundationRole
{
    public override string Name => "九尾狐 副官";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>MTF E-11: Lieutenant</color>";

    public override string Description => "隊長の補佐を目的とし、万一の際は代理・臨時隊長として指示を下せる。";

    public override RoleTypeId BaseRole => RoleTypeId.NtfSergeant;

    public override float? MaxHealth => 100f;

    public override string CustomInfo => "Nine-tailed Fox Lieutenant";

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunE11SR,
        ItemType.KeycardMTFCaptain,
        ItemType.Adrenaline,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.GrenadeFlash,
        ItemType.ArmorHeavy,
        ItemType.Radio,
    ];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 120,
        [ItemType.Ammo9x19] = 130,
    };
}
