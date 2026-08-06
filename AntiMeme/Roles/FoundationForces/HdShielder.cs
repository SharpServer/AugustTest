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

/// <summary>大型シールドを持つ Nu-7 シールド兵です。</summary>
public class HdShielder : FoundationRole
{
    public override string Name => "ハンマーダウン シールド兵";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#353535>MTF Nu-7: Shielder</color>";
    public override string Description => "大型シールドで部隊を先導し、シールドが破損するまで敵の攻撃を防ぐ。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;
    public override float? MaxHealth => 120f;
    public override string CustomInfo => "<color=#727472>Hammer Down Shielder</color>";
    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.KeycardMTFOperative,
        ItemType.Medkit,
        ItemType.Radio,
        ItemType.Flashlight,
    ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo9x19] = 120,
    };

    public override IReadOnlyList<Type> CustomItems => [typeof(GunFSP18), typeof(ArmorInfantry)];

    private const float ShieldMaxValue = 100f;
    private const float Scp049ShieldDamage = 50f;
    private float shield;

    protected override void OnSpawned()
    {
        shield = ShieldMaxValue;

        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);
        Hook(
            () => Scp049Events.Attacking += OnScp049Attacking,
            () => Scp049Events.Attacking -= OnScp049Attacking);
        Hook(
            () => PlayerEvents.Death += OnDeath,
            () => PlayerEvents.Death -= OnDeath);
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!ev.IsAllowed || !IsMine(ev.Player) || shield <= 0f)
            return;

        if (ev.Attacker is { IsSCP: true } && ev.DamageHandler is ScpDamageHandler instant && instant.Damage < 0f)
        {
            ev.IsAllowed = false;
            instant.Damage = 0f;
            shield = 0f;
            return;
        }

        if (ev.DamageHandler is not StandardDamageHandler damage || damage.Damage <= 0f)
            return;

        float absorbed = Math.Min(shield, damage.Damage);
        shield -= absorbed;
        damage.Damage -= absorbed;
    }

    private void OnScp049Attacking(Scp049AttackingEventArgs ev)
    {
        if (!ev.IsAllowed || !ev.InstantKill || !IsMine(ev.Target) || shield <= 0f)
            return;

        ev.IsAllowed = false;
        shield = Math.Max(0f, shield - Scp049ShieldDamage);
    }

    private void OnDeath(PlayerDeathEventArgs ev)
    {
        if (IsMine(ev.Player))
            shield = 0f;
    }
}
