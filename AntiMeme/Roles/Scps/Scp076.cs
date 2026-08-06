using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Abilities;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Utility;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;
using AntiMeme.Roles;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-076, the Omega-7 combatant. The role starts aligned with Foundation
/// personnel and becomes rebellious after three Foundation kills or the loss
/// of every living Omega-7 controller.
/// </summary>
public sealed class Scp076 : ScpRole, IPandraBreakerTarget
{
    private const int ResistanceKillThreshold = 3;
    private const int ResistanceCountdownSeconds = 600;
    private const byte BaseMovementIntensity = 25;
    private const byte BoostedMovementIntensity = 40;

    private int foundationKills;
    private int resistanceRemaining;
    private byte movementIntensity;
    private int omegaCheckGeneration;
    private bool resistance;
    private bool suppressionDetonating;
    private float damageBoostUntil;
    private IDisposable pandraRegistration;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 0 7 6";

    public override string Name => "SCP-076";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff1493>SCP-076</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#00b7eb>The Foundation</color> - <color=#c50000>The SCPs</color>";

    /// <inheritdoc/>
    public override string Objective => "財団の制御下で敵対勢力を殲滅せよ。財団を3人殺害、またはOmega-7全滅で反逆状態になる。";

    public override string Description =>
        "機動部隊Omega-7 \"Pandra's Box\" に運用される、財団制御下の異常戦闘員。\n" +
        "槍とアビリティを使い、財団の敵対勢力を殲滅せよ。\n" +
        "未反逆の間は財団側の勝利に貢献する。\n" +
        "<color=#ff3333>財団職員を3人殺害、またはOmega-7が全滅すると反逆状態となり、10分後に抑制装置が起爆する。</color>";

    protected override float HintDuration => 15f;

    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;

    public override CustomTeam Team =>
        resistance ? CustomTeam.Get<ScpTeam>() : CustomTeam.Get<FoundationForcesTeam>();

    public override Vector3? SpawnPosition => SpawnPoints.NtfEntry;

    public override float? MaxHealth => 1500f;

    public override string CustomInfo => "<color=#ff1493>SCP-076</color>";

    public override IReadOnlyList<Type> CustomItems => [typeof(Spear)];

    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<Scp1853>(),

        // 持ち物は槍で固定。拾い食いも取り落としもさせない。
        RoleEffect.Of<AntiMeme.Effects.InventoryLocked>(),
    ];

    /// <inheritdoc />
    bool IPandraBreakerTarget.IsActive => Player is { IsDestroyed: false, IsAlive: true } &&
                                           ReferenceEquals(CustomRole.Of(Player), this);

    /// <inheritdoc />
    bool IPandraBreakerTarget.IsResistanceState => resistance;

    /// <inheritdoc />
    bool IPandraBreakerTarget.TryDetonateSuppressionDevice() => DetonateSuppressionDevice();

    /// <summary>Returns whether a player currently owns an active SCP-076 role.</summary>
    public static bool IsActiveScp076(Player player) =>
        player is { IsDestroyed: false, IsAlive: true } && CustomRole.Is<Scp076>(player);

    /// <summary>Returns whether a player is in SCP-076's rebellion state.</summary>
    public static bool IsResistanceState(Player player) =>
        CustomRole.Of(player) is Scp076 role && role.resistance;

    /// <summary>Returns whether SCP-076 is still aligned with Foundation victory.</summary>
    public static bool IsFoundationAlignedForVictory(Player player) =>
        CustomRole.Of(player) is Scp076 role && !role.resistance;

    /// <summary>Detonates the active SCP-076 suppression device, if applicable.</summary>
    public static bool TryDetonateSuppressionDevice(Player player) =>
        CustomRole.Of(player) is Scp076 role && role.DetonateSuppressionDevice();

    protected override void OnSpawned()
    {
        movementIntensity = BaseMovementIntensity;
        resistanceRemaining = ResistanceCountdownSeconds;
        foundationKills = 0;
        resistance = false;
        suppressionDetonating = false;
        damageBoostUntil = 0f;

        SetHumeShield(500f);
        BoostHumeShieldRegen(2.5f);

        AbilityBase.Give<AbsolutePowerAbility>(Player);
        AbilityBase.Give<GenerateWeaponAbility>(Player);

        pandraRegistration = PandraBreakerIntegration.Register(this);
        Scope.OnDispose(_ => pandraRegistration?.Dispose());

        Scope.RunLoop(1f, _ => Tick());

        Hook(
            () => PlayerEvents.Death += OnPlayerDeath,
            () => PlayerEvents.Death -= OnPlayerDeath);

        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

        Hook(
            () => PlayerEvents.ChangingRole += OnPlayerChangingRole,
            () => PlayerEvents.ChangingRole -= OnPlayerChangingRole);

        Hook(
            () => PlayerEvents.Left += OnPlayerLeft,
            () => PlayerEvents.Left -= OnPlayerLeft);

        ScheduleOmegaSevenLossCheck();
    }

    protected override void OnRemoved()
    {
        pandraRegistration?.Dispose();
        pandraRegistration = null;

        if (Player is { IsDestroyed: false })
        {
            Player.DisableEffect<MovementBoost>();
        }
    }

    private void OnPlayerDeath(PlayerDeathEventArgs ev)
    {
        if (IsOmegaSevenController(ev.Player))
            ScheduleOmegaSevenLossCheck();

        if (!IsActiveScp076(Player) || !ReferenceEquals(ev.Attacker, Player) ||
            ev.Player is null || ReferenceEquals(ev.Player, Player) ||
            !IsFoundationPersonnel(ev.Player))
            return;

        foundationKills++;

        Scope.Delay(60f, owner =>
        {
            if (!IsActiveScp076(owner)) return;

            damageBoostUntil = Time.time + 30f;
            movementIntensity = BoostedMovementIntensity;
            Scope.Delay(30f, _ => movementIntensity = BaseMovementIntensity);
        });

        if (foundationKills >= ResistanceKillThreshold)
            EnterResistanceState("財団職員を3人殺害したため、あなたは財団に反逆した！");
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (Time.time >= damageBoostUntil || ev.Attacker is not { } attacker ||
            !ReferenceEquals(attacker, Player) || ev.DamageHandler is not PlayerStatsSystem.StandardDamageHandler damage)
            return;

        damage.Damage *= 1.2f;
    }

    private void OnPlayerChangingRole(PlayerChangingRoleEventArgs ev)
    {
        if (ev.Player is not null && IsOmegaSevenController(ev.Player))
            ScheduleOmegaSevenLossCheck();
    }

    private void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        if (ev.Player is not null && IsOmegaSevenController(ev.Player))
            ScheduleOmegaSevenLossCheck();
    }

    private void Tick()
    {
        if (!IsActiveScp076(Player)) return;

        if (Player.TryGetEffect(out MovementBoost movement))
            movement.Intensity = movementIntensity;
        else
            Player.EnableEffect<MovementBoost>(movementIntensity, 2f);

        if (!resistance) return;

        resistanceRemaining = Math.Max(0, resistanceRemaining - 1);
        ShowStatus(
            $"<color=#ff3333><b>[反逆状態]</b></color>\n抑制装置: 起動済み\n起爆まで: {resistanceRemaining / 60:00}:{resistanceRemaining % 60:00}",
            1.2f);

        if (resistanceRemaining == 0)
            DetonateSuppressionDevice();
    }

    private void ScheduleOmegaSevenLossCheck()
    {
        int generation = ++omegaCheckGeneration;

        Scope.Delay(0.1f, _ =>
        {
            if (generation != omegaCheckGeneration || !Round.IsRoundInProgress || HasAliveOmegaSevenController())
                return;

            foreach (Scp076 role in CustomRole.Active.OfType<Scp076>().ToArray())
            {
                if (IsActiveScp076(role.Player) && !role.resistance)
                    role.EnterResistanceState("Omega-7が不在となったため、あなたは財団に反逆した！");
            }
        });
    }

    private void EnterResistanceState(string triggerMessage)
    {
        if (!IsActiveScp076(Player) || resistance) return;

        resistance = true;
        resistanceRemaining = ResistanceCountdownSeconds;
        Player.CustomInfo = "<color=#ff3333>SCP-076</color>";
        Player.SendHint(
            $"<size=26><color=red><b>※{triggerMessage}\nロール名が赤くなりました。\n10分後に抑制装置が起爆し爆死します！</b></color></size>",
            8f);
    }

    private bool DetonateSuppressionDevice()
    {
        if (!IsActiveScp076(Player) || !resistance || suppressionDetonating)
            return false;

        suppressionDetonating = true;
        Player.Kill("抑制装置により爆発された");

        return true;
    }

    private static bool HasAliveOmegaSevenController() =>
        Player.ReadyList.Any(player => IsOmegaSevenController(player) && player.IsAlive);

    private static bool IsOmegaSevenController(Player player) =>
        player is { IsDestroyed: false } &&
        (CustomRole.Is<PdxWarden>(player) || CustomRole.Is<PdxWatcher>(player));

    private static bool IsFoundationPersonnel(Player player)
    {
        if (player is not { IsDestroyed: false }) return false;

        if (CustomRole.Of(player) is Scp076)
            return false;

        CustomTeam team = CustomRole.Of(player)?.Team;
        return team == CustomTeam.Get<FoundationForcesTeam>() ||
               team == CustomTeam.Get<GuardsTeam>() ||
               team == CustomTeam.Get<ScientistsTeam>() ||
               player.Role is RoleTypeId.NtfSpecialist or RoleTypeId.NtfSergeant or
                   RoleTypeId.NtfCaptain or RoleTypeId.NtfPrivate or
                   RoleTypeId.FacilityGuard or RoleTypeId.Scientist;
    }
}
