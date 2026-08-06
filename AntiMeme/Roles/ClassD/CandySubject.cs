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

public sealed class CandySubject : ClassDRole
{
    public override string Name => "菓子被験者";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ee7600>Candy Subject</color>";

    /// <inheritdoc/>
    public override string Objective => "キャンディーを活用しながら、施設から脱出せよ。";
    public override string Description => "お菓子が大好きな変な博士の実験に巻き込まれた可愛そうなDクラス職員。\nいっぱいキャンディーを持たされている。";
    public override RoleTypeId BaseRole => RoleTypeId.ClassD;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom(MapGeneration.RoomName.Lcz330, Vector3.up * 0.8f);
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Candy Subject";
    public override IReadOnlyList<ItemType> Items => [ ItemType.KeycardJanitor, ItemType.SCP330 ];
}
