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

/// <summary>Omega-1 の調査官です。</summary>
public class LwsForensic : FoundationRole
{
    public override string Name => "Law's Left Hand: Forensic";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#A0A0A0><b>MTF Omega-1: Forensic</b></color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#00b7eb>The Foundation</color> - <color=#0000c8>Global Occult Coalition</color>";

    /// <inheritdoc/>
    public override string Objective => "<b>法のために、秩序の暴走を許さず施設を守護せよ。</b>";
    public override string Description => "Omega-1の調査官。\n施設内の脅威を見極め、部隊の生存を支援せよ。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSpecialist;
    public override float? MaxHealth => 100f;
    public override Vector3? SpawnPosition => SpawnPoints.FirstTeam;
    public override string CustomInfo => $"<color={FoundationColors.Silver}>Law's Left Hand Forensic</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFOperative,
        ItemType.GunCOM18,
        ItemType.Flashlight,
        ItemType.Medkit,
        ItemType.Adrenaline,
        ItemType.Radio,
        ItemType.ArmorCombat,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(S41MedicalPistol)];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo9x19] = 120,
    };
}
