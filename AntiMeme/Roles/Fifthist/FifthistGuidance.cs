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

public sealed class FifthistGuidance : FifthistRole
{
    public override string Name => "第五教会 案内人";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff00fa>Fifthist: Guidance</color>";

    /// <inheritdoc/>
    public override string Objective => "杖を用い、第五主義を施設に広めなさい。あなたの導きは教会にとって重要です！";
    public override string Description => "第五主義を広め、人々を第五世界へと誘う案内人。\n杖を使って相手を第五すると第五主義者に出来る。";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? SpawnPosition => SpawnPoints.ExitB;
    public override float? MaxHealth => 125f;
    public override string CustomInfo => "<color=#FF0090>Fifthist Guidance</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.ArmorLight, ItemType.Medkit, ItemType.Adrenaline, ItemType.SCP500, ItemType.GrenadeHE ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(CaneOfTheStars), typeof(KeycardFifthist) ];
}
