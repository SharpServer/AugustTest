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

/// <summary>Nu-7 を率いる元帥です。</summary>
public class HdMarshal : FoundationRole
{
    public override string Name => "ハンマーダウン 元帥";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#151515>MTF Nu-7: Marshal</color>";
    public override string Description => "Nu-7の師団を指揮し、勝利へと導く。\n敗北など許されない。突き進め！";
    public override RoleTypeId BaseRole => RoleTypeId.NtfCaptain;
    public override float? MaxHealth => 180f;
    public override string CustomInfo => "<color=#727472>Hammer Down Marshal</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFCaptain,
        ItemType.GrenadeHE,
        ItemType.GrenadeHE,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems =>
    [
        typeof(SerumC),
        typeof(AdvancedMedkit),
        typeof(ArmorVip),
        typeof(GunN7Weltkrieg),
    ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 250,
    };
}
