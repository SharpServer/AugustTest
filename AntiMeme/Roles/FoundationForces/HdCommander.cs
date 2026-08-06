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

/// <summary>Nu-7 Hammer Down の指揮官です。</summary>
public class HdCommander : FoundationRole
{
    public override string Name => "ハンマーダウン 指揮官";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#252525>MTF Nu-7: Commander</color>";
    public override string Description => "Nu-7の歩兵たちを指揮し、制圧を進める。\n偉大なる我らが元帥の指示に従え！";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSergeant;
    public override float? MaxHealth => 125f;
    public override string CustomInfo => "<color=#727472>Hammer Down Commander</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFOperative,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.GrenadeHE,
        ItemType.GrenadeHE,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(ArmorVip), typeof(GunN7CR)];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 200,
    };
}
