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

/// <summary>See No Evil の破力兵です。</summary>
public class SneNeutralitist : FoundationRole
{
    public override string Name => "シー・ノー・イービル 破力兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#FF1493>MTF Eta-10: Neutralitist</color>";
    public override string Description => "気狂いどもを食い止めろ！";
    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;
    public override float? MaxHealth => 125f;
    public override string CustomInfo => "<color=#FF1493>See No Evil Neutralitist</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunE11SR,
        ItemType.KeycardMTFOperative,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.Radio,
        ItemType.ArmorCombat,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(NeutralizeGrenade), typeof(NeutralizeGrenade)];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 120,
    };

    protected override void OnSpawned() => Hook(
        () => PlayerEvents.Hurting += OnHurting,
        () => PlayerEvents.Hurting -= OnHurting);

    private void OnHurting(PlayerHurtingEventArgs ev) => SneDamageRules.ReduceFifthistDamage(ev, this);
}
