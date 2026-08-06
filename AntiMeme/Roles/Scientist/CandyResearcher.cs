using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Scientist;

public sealed class CandyResearcher : ScientistRole
{
    public override string Name => "お菓子研究者";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#faff86>Candy Researcher</color>";

    /// <inheritdoc/>
    public override string Objective => "キャンディーを活用しながら、施設から脱出せよ。";
    public override string Description => "兎に角甘いものが好きな科学者。\nキャンディー大好き！";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Candy Researcher";
    public override IReadOnlyList<ItemType> Items => [ ItemType.KeycardScientist, ItemType.SCP330 ];
}
