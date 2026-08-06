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

/// <summary>Alpha-1 の監督官です。</summary>
public class RrhWarden : FoundationRole
{
    public override string Name => "Red Right Hand: Warden";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#C50000><b>MTF Alpha-1: Warden</b></color>";

    /// <inheritdoc/>
    public override string Objective => "<b>秩序のために、必要な犠牲を厭わず施設を掌握せよ。</b>";
    public override string Description => $"Alpha-1の監督官。\nO5の意思として現場を制圧し、財団側の勝利を確実にせよ。\n<color={FoundationColors.Crimson}>高位の職員</color>を脱出させろ。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfCaptain;
    public override float? MaxHealth => 130f;
    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;
    public override string CustomInfo => $"<color={FoundationColors.Red}>Red Right Hand Warden</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFCaptain,
        ItemType.GrenadeHE,
        ItemType.GrenadeFlash,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(ArmorVip), typeof(GunFRMGX)];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 260,
    };
}
