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

/// <summary>Nu-7 の標準歩兵です。</summary>
public class HdInfantry : FoundationRole
{
    public override string Name => "ハンマーダウン 歩兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#353535>MTF Nu-7: Infantry</color>";
    public override string Description => "Nu-7の最下級兵だが、それでも強い装備が持たされている。\nNu-7とはこういう奴らなのだ";
    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;
    public override float? MaxHealth => 110f;
    public override string CustomInfo => "<color=#727472>Hammer Down Infantry</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunCrossvec,
        ItemType.KeycardMTFOperative,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.GrenadeFlash,
        ItemType.GrenadeHE,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(ArmorInfantry)];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo9x19] = 140,
    };
}
