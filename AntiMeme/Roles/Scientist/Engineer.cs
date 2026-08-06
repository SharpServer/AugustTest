using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Scientist;

public sealed class Engineer : ScientistRole
{
    public override string Name => "エンジニア";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#faff86>Engineer</color>";

    /// <inheritdoc/>
    public override string Objective => "様々なタスクをこなし、最強の弾頭を起動せよ！";
    public override string Description => "施設内の様々なシステム等を整備する職員。\nToolboxを用いてSCPを食い止めたり、他職員の脱出等をサポートせよ。";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Engineer";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardContainmentEngineer, ItemType.Medkit, ItemType.Medkit ];
}
