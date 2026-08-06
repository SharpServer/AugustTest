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

/// <summary>Pandra's Box の監視員です。</summary>
public class PdxWatcher : FoundationRole
{
    public override string Name => "Pandra's Box: Watcher";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#960018><b>Pandra's Box: Watcher</b></color>";

    /// <inheritdoc/>
    public override string Objective => "Wardenを補助し、アベルの異常を察知したら迅速に報告せよ。";
    public override string Description => "Wardenの補助を行い、アベルを監視する。\n<b>異常を感知したら迅速に上官に連絡する事。</b>";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSergeant;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => $"<color={FoundationColors.Carmine}>Pandra's Box Watcher</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunE11SR,
        ItemType.KeycardMTFCaptain,
        ItemType.Adrenaline,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.GrenadeFlash,
        ItemType.ArmorHeavy,
        ItemType.Radio,
    ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 130,
    };
}
