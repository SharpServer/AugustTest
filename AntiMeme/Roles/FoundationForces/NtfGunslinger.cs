using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// アサルトライフルと弾倉式グレネードランチャーの複合火器を扱うライフルマン。
/// </summary>
public class NtfGunslinger : FoundationRole
{
    public override string Name => "九尾狐 銃撃兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>MTF E-11: Gunslinger</color>";

    public override string Description =>
        "<size=23>NTFの中でも特にアサルトライフルの扱いに長け、新兵器のテスターとして抜擢されたライフルマン。\n" +
        "アサルトライフルと弾倉式グレネードランチャーが一体化した\n" +
        "マルチウェポン\"XE-11K MR\"を扱い、戦場でのあらゆる状況に対応する</size>";

    public override RoleTypeId BaseRole => RoleTypeId.NtfSergeant;

    public override float? MaxHealth => 100f;

    public override string CustomInfo => "Nine-tailed Fox Gunslinger";

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFOperative,
        ItemType.Medkit,
        ItemType.Radio,
        ItemType.Flashlight,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(GunXE11KMR), typeof(ArmorInfantry)];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 120,
    };
}
