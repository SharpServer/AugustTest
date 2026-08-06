using AntiMeme.Roles;
using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Scientist;

public sealed class FacilityManager : ScientistRole
{
    public override string Name => "施設管理官";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#dc143c>Facility Manager</color>";

    /// <inheritdoc/>
    public override string Objective => "施設からの脱出を目指しながら、サイトの行く末を監督せよ";
    public override string Description => "施設を管理・運営する重要な科学者。\n区画管理官や警備員たちに指示を出し、収容違反に対処する。";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;

    /// <summary>マップ側のマーカーで指定します。マーカーが無ければバニラの地点です。</summary>
    public override Vector3? SpawnPosition => SpawnPoints.Tagged("FacilityManagerSpawnPoint");
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Facility Manager";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCrossvec, ItemType.KeycardFacilityManager, ItemType.Medkit,
      ItemType.Medkit, ItemType.ArmorHeavy, ItemType.Radio ];
}
