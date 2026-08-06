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

public sealed class WaterWarrior : WarriorRole
{
    public override string Name => "WATER WARRIOR";

    /// <inheritdoc/>
    public override string HudLabel => "<b><color=#00FFFF>WATER WARRIOR</color></b>";

    /// <inheritdoc/>
    public override string TeamLabel => "<b><color=#00FFFF>WATER WARRIOR's DIVISION</color></b>";

    /// <inheritdoc/>
    public override string Objective => "水鉄砲を使い、施設を水没まで追い込め！！！！！";
    public override string Description => "<size=24><color=#00ffff>夏にヒャッハーしてる謎の勢力。</color>\n水鉄砲を使って施設を水没まで追い込め！！！！！</size>";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? SpawnPosition => SpawnPoints.ChaosEntry;
    public override float? MaxHealth => 500f;
    public override string CustomInfo => "<color=#00ffff>WATER WARRIOR</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.SCP1509, ItemType.ArmorHeavy, ItemType.SCP500,
      ItemType.SCP500, ItemType.KeycardO5 ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 50, [ItemType.Ammo556x45] = 220 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Slowness>(10) ];

    protected override void OnSpawned()
    {
        RoleWear wear = RoleWear.AttachSchematic(Player, "WaterWarriorsModel");
        if (wear != null)
        {
            wear.WithLight(Color.black);
            Scope.OnDispose(_ => wear.Dispose());
        }

        AbilityBase.Give<AquaJumpAbility>(Player);
        AbilityBase.Give<AquaSplashAbility>(Player);
    }
}
