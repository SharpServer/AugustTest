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

/// <summary>ショットガン二丁を持つ Nu-7 砲弾兵です。</summary>
public class HdShotgunner : FoundationRole
{
    public override string Name => "ハンマーダウン 砲弾兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#353535>MTF Nu-7: Shotgunner</color>";
    public override string Description => "ショットガンを二丁持ちしたNu-7の歩兵。\n素早い猛攻で敵を粉砕する。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;
    public override float? MaxHealth => 110f;
    public override string CustomInfo => "<color=#727472>Hammer Down Shotgunner</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.GunShotgun,
        ItemType.GunShotgun,
        ItemType.KeycardMTFOperative,
        ItemType.Adrenaline,
        ItemType.Medkit,
        ItemType.Radio,
    ];
    public override IReadOnlyList<Type> CustomItems => [typeof(ArmorInfantry)];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo12gauge] = 200,
    };
}
