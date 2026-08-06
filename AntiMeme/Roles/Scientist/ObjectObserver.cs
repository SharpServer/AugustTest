using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Scientist;

public sealed class ObjectObserver : ScientistRole
{
    public override string Name => "オブジェクト観測者";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#faff86>Object Observer</color>";

    /// <inheritdoc/>
    public override string Objective => "オブジェクトに注意しながら、施設から脱出せよ。";
    public override string Description => "SCPオブジェクトの状況を監視し、報告する。";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.Lcz173, new Vector3(0f, 0.35f, 0f));
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Object Observer";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardScientist, ItemType.Medkit, ItemType.ArmorLight ];
}
