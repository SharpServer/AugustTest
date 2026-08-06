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
using AntiMeme.Roles;
using AntiMeme.Roles.Scps;

namespace AntiMeme.Roles.FoundationForces;

/// <summary>SCP-173 の土台を持つ財団側の Sculpture です。</summary>
public class Sculpture : FoundationRole
{
    public override string Name => "Sculpture";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00b7eb>Sculpture</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#00b7eb>The Foundation</color>";

    /// <inheritdoc/>
    public override string Objective => "財団に従い、人類を根絶させよ。";
    public override string Description => "相手が瞬きしたときに高速で移動し、痛めつける。\n財団の味方である。";
    public override RoleTypeId BaseRole => RoleTypeId.Scp173;
    public override float? MaxHealth => 500f;
    public override Vector3? SpawnPosition => SpawnPoints.NtfEntry;
    public override Vector3? Scale => new Vector3(0.8f, 1f, 0.8f);
    public override string CustomInfo => "<color=#00B7EB>Sculpture</color>";
    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<Slowness>(20),
    ];

    protected override void OnSpawned()
    {
        SetHumeShield(300f);

        // 173 の瞬きイベントは LabApi に無いため、既存 SCP 役職と同じ EXILED ブリッジを使う。
        Hook(
            () => ExiledScp173.Blinking += OnBlinking,
            () => ExiledScp173.Blinking -= OnBlinking);
        Hook(
            () => ExiledScp173.AddingObserver += OnAddingObserver,
            () => ExiledScp173.AddingObserver -= OnAddingObserver);
        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);
    }

    private void OnBlinking(BlinkingEventArgs ev)
    {
        if (IsMine(ev.Player.ReferenceHub) && ev.Targets.Count >= 3)
            ev.Scp173.BlinkReady = false;
    }

    private void OnAddingObserver(AddingObserverEventArgs ev)
    {
        if (!IsMine(ev.Player.ReferenceHub)) return;

        if (CustomTeam.Get<FoundationForcesTeam>().Includes(ev.Observer) ||
            CustomTeam.Get<GuardsTeam>().Includes(ev.Observer))
            ev.IsAllowed = false;
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!IsMine(ev.Attacker) || ev.DamageHandler is not ScpDamageHandler damage || damage.Damage >= 0f)
            return;

        ev.IsAllowed = false;
        if (ev.Player is { IsAlive: true })
            ev.Player.Damage(35f, Player);
    }

    // 旧実装の死亡時 CASSIE クリアは Audio ドメインの常駐ハンドラへ接続する。
}
