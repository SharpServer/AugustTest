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
using AntiMeme.Roles.Scps;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>ミームで構成された機動部隊アラ・オルンです。</summary>
public class AraOrun : FoundationRole
{
    private const float MaxEnergy = 200f;

    public override string Name => "アラ・オルン";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ffff00>MTF Omega-0: Ará Orún</color>";

    /// <inheritdoc/>
    public override string Objective => "マリオン・ホイーラーを手助けし、反ミーム爆弾へと導け！";
    public override string Description => "貴方はミームで構成された機動部隊、アラ・オルンだ。\n下層を目指す反ミーム部門職員を<color=cyan>サポート</color>し\nSCP-3125とその傀儡を<color=red>食い止めよ</color>！";
    public override RoleTypeId BaseRole => RoleTypeId.Scp079;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Ara Orun";

    protected override void OnSpawned()
    {
        if (Player.RoleBase is not Scp079Role role) return;

        if (role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tiers))
        {
            int[] thresholds = tiers.AbsoluteThresholds;
            tiers.TotalExp = thresholds[Mathf.Clamp(5 - 2, 0, thresholds.Length - 1)];
        }

        if (role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager aux))
        {
            aux._maxPerTier[aux._tierManager.AccessTierIndex] = MaxEnergy;
            aux.CurrentAux = MaxEnergy;
        }
    }

}
