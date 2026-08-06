using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using AntiMeme.Maps;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Fifthist;

public sealed class FifthistMindblaster : FifthistRole
{
    public override string Name => "第五教会 思壊師";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff00fa>Fifthist: Mindblaster</color>";

    /// <inheritdoc/>
    public override string Objective => "第五の力を用いて、全ての思考を第五で破壊しろ！";
    public override string Description => "<color=#ff00fa>第五の力</color>を用いて敵の思念を破壊する";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? SpawnPosition => SpawnPoints.ExitB;
    public override float? MaxHealth => 155f;
    public override string CustomInfo => "<color=#FF0090>Fifthist Mindblaster</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCOM18, ItemType.ArmorLight, ItemType.Medkit, ItemType.GrenadeHE ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(KeycardFifthist), typeof(Mindblaster) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo762x39] = 80 };
}
