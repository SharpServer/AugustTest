using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// 対物ライフルでオブジェクトを無力化する戦術スペシャリスト。
/// </summary>
public class NtfSpecialist : FoundationRole
{
    public override string Name => "九尾狐 スペシャリスト";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>MTF E-11: Specialist</color>";

    public override string Description =>
        "九尾狐の中でもとてもオブジェクト達に精通している戦術スペシャリスト。\n" +
        "専用の対物ライフルでオブジェクトを無力化する。";

    public override RoleTypeId BaseRole => RoleTypeId.NtfSpecialist;

    public override float? MaxHealth => 100f;

    public override string CustomInfo => "Nine-tailed Fox Specialist";

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunCOM18,
        ItemType.KeycardMTFCaptain,
        ItemType.Medkit,
        ItemType.Medkit,
        ItemType.GrenadeHE,
        ItemType.ArmorHeavy,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(GunM82)];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 180,
        [ItemType.Ammo9x19] = 120,
    };
}
