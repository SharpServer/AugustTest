using System.Collections.Generic;
using AntiMeme.Abilities;
using AntiMeme.Maps;
using CustomPlayerEffects;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Warriors;

public sealed class CandyWarriorHalloween : WarriorRole
{
    public override string Name => "CANDY WARRIOR";

    /// <inheritdoc/>
    public override string HudLabel => "<b><color=#ffffff>CANDY WARRIER</color></b>";

    /// <inheritdoc/>
    public override string TeamLabel => "<b><color=#ffffff>CANDY WARRIER's DIVISION</color></b>";

    /// <inheritdoc/>
    public override string Objective => "全施設にFunnyなお菓子の正義を執行しろ";
    public override string Description => "非常に<color=#EE7600>お菓子的</color>である。そうは思わんかね？";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? SpawnPosition => SpawnPoints.ChaosEntry;
    public override float? MaxHealth => 1000f;
    public override string CustomInfo => "<color=#EE7600>CANDY WARRIOR</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.SCP1509, ItemType.GunCOM18, ItemType.ArmorHeavy,
      ItemType.SCP500, ItemType.SCP500, ItemType.KeycardO5, ItemType.SCP330 ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 50 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Slowness>(10) ];

    protected override void OnSpawned()
    {
        RoleWear wear = RoleWear.AttachSchematic(Player, "CandyWarrier");
        if (wear != null) Scope.OnDispose(_ => wear.Dispose());
    }
}
