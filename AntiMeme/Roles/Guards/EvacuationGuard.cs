using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Guards;

public sealed class EvacuationGuard : GuardRole
{
    public override string Name => "下層避難支援警備隊員";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>Emergency Evacuation Guard</color>";

    /// <inheritdoc/>
    public override string Objective => "職員達を上部階層へ避難させ、施設の秩序を守護せよ。";
    public override string Description => "下層の秩序を守り、職員の避難を助ける。";
    public override RoleTypeId BaseRole => RoleTypeId.FacilityGuard;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.LczArmory, Vector3.up);
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Emergency Evacuation Guard";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunFSP9, ItemType.KeycardGuard, ItemType.Medkit,
      ItemType.Painkillers, ItemType.ArmorCombat, ItemType.Radio ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 150 };
}
