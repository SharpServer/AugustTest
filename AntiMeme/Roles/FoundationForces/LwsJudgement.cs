using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;

using ExiledScp173 = Exiled.Events.Handlers.Scp173;
using BlinkingEventArgs = Exiled.Events.EventArgs.Scp173.BlinkingEventArgs;
using AddingObserverEventArgs = Exiled.Events.EventArgs.Scp173.AddingObserverEventArgs;
using Scp049AttackingEventArgs = LabApi.Events.Arguments.Scp049Events.Scp049AttackingEventArgs;
using AntiMeme.Roles;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>Omega-1 を率いる裁定担当です。</summary>
public class LwsJudgement : FoundationRole
{
    public override string Name => "Law's Left Hand: Judgement";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#A0A0A0><b>MTF Omega-1: Judgement</b></color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#00b7eb>The Foundation</color> - <color=#0000c8>Global Occult Coalition</color>";

    /// <inheritdoc/>
    public override string Objective => "<b>法のために、秩序の暴走を許さず施設を守護せよ。</b>";
    public override string Description => "Omega-1を率いる裁定担当。\n財団の秩序を回復し、敵対勢力を排除せよ。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfCaptain;
    public override float? MaxHealth => 120f;
    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;
    public override string CustomInfo => $"<color={FoundationColors.Silver}>Law's Left Hand Judgement</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFCaptain,
        ItemType.GunE11SR,
        ItemType.GrenadeHE,
        ItemType.GrenadeFlash,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(ArmorVip)];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 220,
        [ItemType.Ammo9x19] = 80,
    };
}
