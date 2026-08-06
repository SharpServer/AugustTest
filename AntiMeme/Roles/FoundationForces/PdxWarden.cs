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

namespace AntiMeme.Roles.FoundationForces;

/// <summary>Pandra's Box の監督官です。</summary>
public class PdxWarden : FoundationRole
{
    public override string Name => "Pandra's Box: Warden";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#960018><b>Pandra's Box: Warden</b></color>";

    /// <inheritdoc/>
    public override string Objective => "アベルを監視し、暴走時には抑制装置を起動して制御せよ。";
    public override string Description => "Omega-7の監督官。\n<b>アベルを監視し、暴走時には起爆スイッチを押して制御する事。</b>";
    public override RoleTypeId BaseRole => RoleTypeId.NtfCaptain;
    public override float? MaxHealth => 130f;
    public override string CustomInfo => $"<color={FoundationColors.Carmine}>Pandra's Box Warden</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFCaptain,
        ItemType.GrenadeHE,
        ItemType.GrenadeFlash,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.GunFRMG0,
    ];
    public override IReadOnlyList<Type> CustomItems =>
    [
        typeof(PandraBreaker),
        typeof(ArmorVip),
        typeof(GunFRMGX),
    ];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 260,
    };
}
