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

/// <summary>旧 See No Evil の第五教会ダメージ軽減規則を共有します。</summary>
internal static class SneDamageRules
{
    private const float FifthistDamageMultiplier = 0.77f;

    public static void ReduceFifthistDamage(PlayerHurtingEventArgs ev, CustomRole owner)
    {
        if (ev.Player is null || ev.Attacker is null || !ReferenceEquals(CustomRole.Of(ev.Player), owner))
            return;

        if (CustomRole.Of(ev.Attacker)?.Team != CustomTeam.Get<FifthistTeam>())
            return;

        if (ev.DamageHandler is StandardDamageHandler damage)
            damage.Damage *= FifthistDamageMultiplier;
    }
}
