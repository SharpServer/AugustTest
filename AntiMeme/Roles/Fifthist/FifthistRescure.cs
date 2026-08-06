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

public sealed class FifthistRescure : FifthistRole
{
    public override string Name => "第五教会 救出師";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff00fa>Fifthist: Rescue</color>";

    /// <inheritdoc/>
    public override string Objective => "第五を探し出し、救出し、従い、施設を占領せよ。";
    public override string Description => "非常に<color=#ff00fa>第五的</color>な存在を脱出させなければいけない";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? SpawnPosition => SpawnPoints.ExitB;
    public override float? MaxHealth => 135f;
    public override string CustomInfo => "<color=#FF0090>Fifthist Rescure</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCOM18, ItemType.ArmorCombat, ItemType.Medkit,
      ItemType.Adrenaline, ItemType.SCP500, ItemType.GrenadeHE ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(KeycardFifthist) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo762x39] = 180 };
}
