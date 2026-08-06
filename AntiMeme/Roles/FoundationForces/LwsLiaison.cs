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

/// <summary>Omega-1 の連絡官です。</summary>
public class LwsLiaison : FoundationRole
{
    public override string Name => "Law's Left Hand: Liaison";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#A0A0A0><b>MTF Omega-1: Liaison</b></color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#00b7eb>The Foundation</color> - <color=#0000c8>Global Occult Coalition</color>";

    /// <inheritdoc/>
    public override string Objective => "<b>法のために、秩序の暴走を許さず施設を守護せよ。</b>";
    public override string Description => "Omega-1の連絡官。\n現場の味方と連携し、重要人物の保護と敵対勢力の排除を遂行せよ。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSergeant;
    public override float? MaxHealth => 110f;
    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;
    public override string CustomInfo => $"<color={FoundationColors.Silver}>Law's Left Hand Liaison</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFCaptain,
        ItemType.GunCrossvec,
        ItemType.GrenadeFlash,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.Radio,
        ItemType.ArmorHeavy,
    ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo9x19] = 180,
    };
}
