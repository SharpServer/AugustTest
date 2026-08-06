using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Guards;

public sealed class SecurityChief : GuardRole
{
    public override string Name => "警備主任";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>Security Chief</color>";

    /// <inheritdoc/>
    public override string Objective => "職員達を地上へ脱出させ、施設の秩序を守護せよ。";
    public override string Description => "施設内の職員を外に脱出させよう！";
    public override RoleTypeId BaseRole => RoleTypeId.FacilityGuard;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Security Chief";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.Medkit, ItemType.Medkit, ItemType.ArmorHeavy, ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(KeycardSecurityChief), typeof(GunFSP18) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 130 };
}
