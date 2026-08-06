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

/// <summary>Alpha-1 の突入要員です。</summary>
public class RrhAssaulter : FoundationRole
{
    public override string Name => "Red Right Hand: Assaulter";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#C50000><b>MTF Alpha-1: Assaulter</b></color>";

    /// <inheritdoc/>
    public override string Objective => "<b>秩序のために、必要な犠牲を厭わず施設を掌握せよ。</b>";
    public override string Description => $"Alpha-1の突入要員。\n強襲で敵の戦線を崩し、財団の優位を作れ。\n<color={FoundationColors.Crimson}>高位の職員</color>を脱出させろ。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;
    public override float? MaxHealth => 110f;
    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;
    public override string CustomInfo => $"<color={FoundationColors.Red}>Red Right Hand Assaulter</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFOperative,
        ItemType.GunCrossvec,
        ItemType.GrenadeHE,
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
