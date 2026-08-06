using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>
/// 機動部隊 Epsilon-11「九尾狐」。装備を宣言するだけの役職群です。
///
/// <para>バニラアイテムは <see cref="Items"/>、実装済みのカスタムアイテムは
/// <see cref="CustomItems"/> で型として宣言します。</para>
/// </summary>
public class NtfCadet : FoundationRole
{
    public override string Name => "九尾狐 士官候補生";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>MTF E-11: Cadet</color>";

    public override string Description =>
        "MTFの士官養成課程を受講し訓練に励んでいる士官候補生。\n" +
        "本来まだ訓練中だがサイト-02の死守の為に保安部隊の隊長として緊急動員された。\n" +
        "その為装備が一世代前の型落ち品となっている。";

    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;

    public override float? MaxHealth => 100f;

    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;

    public override string CustomInfo => "Nine-tailed Fox Cadet";

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFPrivate,
        ItemType.ArmorCombat,
        ItemType.Medkit,
        ItemType.Radio,
        ItemType.Flashlight,
    ];

    public override IReadOnlyList<Type> CustomItems => [typeof(GunProject90)];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo9x19] = 120,
    };
}
