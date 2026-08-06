using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Melee;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.ClassD;

public sealed class Janitor : ClassDRole
{
    public override string Name => "用務員";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ee7600>Janitor</color>";

    /// <inheritdoc/>
    public override string Objective => "施設から脱出せよ。また、汚物をグレネードで清掃せよ。";
    public override string Description => "特殊グレネードで近くの汚れを清掃できる";
    public override RoleTypeId BaseRole => RoleTypeId.ClassD;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom(MapGeneration.RoomName.Lcz173, new Vector3(0f, 0.35f, 0f));
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Janitor";
    public override IReadOnlyList<ItemType> Items => [ ItemType.KeycardJanitor, ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(FakeGrenade), typeof(FakeGrenade), typeof(FakeGrenade),
      typeof(FakeGrenade), typeof(FakeGrenade), typeof(FakeGrenade) ];
}
