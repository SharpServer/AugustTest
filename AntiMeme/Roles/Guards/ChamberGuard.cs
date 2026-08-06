using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Guards;

public sealed class ChamberGuard : GuardRole
{
    public override string Name => "収容室警備";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>Chamber Guard</color>";

    /// <inheritdoc/>
    public override string Objective => "Dクラスとオブジェクトに注意し、確実に職員達を避難させよ。";
    public override string Description => "Dクラス職員やオブジェクトの異常を監視する。";
    public override RoleTypeId BaseRole => RoleTypeId.FacilityGuard;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.Lcz173, new Vector3(0f, 0.35f, 0f));
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Chamber Guard";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunFSP9, ItemType.KeycardGuard, ItemType.Medkit,
      ItemType.ArmorLight, ItemType.Radio ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 100 };
}
