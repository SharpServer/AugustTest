using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Scientist;

public sealed class ZoneManager : ScientistRole
{
    public override string Name => "区画管理官";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00ffff>Zone Manager</color>";

    /// <inheritdoc/>
    public override string Objective => "施設からの脱出を目指しながら、警備職員達を監督せよ";
    public override string Description => "各区画に割り当てられた軽度な権限をもつ科学者";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Zone Manager";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunFSP9, ItemType.KeycardZoneManager, ItemType.KeycardScientist,
      ItemType.Medkit, ItemType.ArmorLight, ItemType.Radio ];
}
