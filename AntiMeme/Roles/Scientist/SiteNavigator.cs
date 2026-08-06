using AntiMeme.Items.Utility.Snav;
using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Scientist;

public sealed class SiteNavigator : ScientistRole
{
    public override string Name => "サイトナビゲーター";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#faff86>Site Navigator</color>";

    /// <inheritdoc/>
    public override string Objective => "S-NAVを活用し、施設から脱出せよ。";
    public override string Description => "携帯用マップ端末\"S-NAV\"を持った研究員。\nつねに構造が変化し続けるサイト-02において、S-NAVは必需品である";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Site Navigator";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.Flashlight, ItemType.Medkit ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(SNAV300), typeof(KeycardSiteNavigator) ];
}
