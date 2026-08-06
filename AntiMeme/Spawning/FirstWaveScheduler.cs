using System;
using System.Linq;
using AntiMeme.Spawning.Waves;
using AntiMeme.Teams.Factions;
using Exiled.API.Enums;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;

namespace AntiMeme.Spawning;

/// <summary>
/// Schedules the initial Security Team / Chaos Agents reinforcement before the
/// normal repeating wave cycle begins.
/// </summary>
public sealed class FirstWaveScheduler : EventHandlerBase
{
    private const float DefaultWaitSeconds = 240f;

    private CoroutineHandle timer;
    private Guid overrideRegistration;
    private bool requested;

    public static float WaitSeconds { get; set; } = DefaultWaitSeconds;
    public static float ElapsedSeconds { get; private set; }
    public static bool HasSpawned { get; private set; }

    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    public override void RegisterEvents()
    {
        Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
        Exiled.Events.Handlers.Server.RestartingRound += Reset;
        Exiled.Events.Handlers.Server.WaitingForPlayers += Reset;
        SpawnSystem.Spawned += OnWaveSpawned;
    }

    public override void UnregisterEvents()
    {
        Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
        Exiled.Events.Handlers.Server.RestartingRound -= Reset;
        Exiled.Events.Handlers.Server.WaitingForPlayers -= Reset;
        SpawnSystem.Spawned -= OnWaveSpawned;
        Reset();
    }

    private void OnRoundStarted()
    {
        Reset();
        RegisterOverrides();
        timer = Timing.RunCoroutine(RunTimer());
    }

    private System.Collections.Generic.IEnumerator<float> RunTimer()
    {
        while (!HasSpawned)
        {
            if (SpawnContext.Active is not DefaultContext)
            {
                CancelOverrides();
                yield break;
            }

            ElapsedSeconds += 0.1f;
            if (!requested && ElapsedSeconds >= Math.Max(0f, WaitSeconds))
            {
                CancelOverrides();
                RegisterOverrides();
                Exiled.API.Features.Respawn.AdvanceTimer(IsSecurityTeamExpected() ? SpawnableFaction.NtfWave : SpawnableFaction.ChaosWave, 999f);
                requested = true;
            }

            yield return Timing.WaitForSeconds(0.1f);
        }
    }

    public static bool IsSecurityTeamExpected()
    {
        int hostile = Player.ReadyList.Count(player =>
            IsCountable(player) &&
            (CustomTeam.Get<ChaosInsurgencyTeam>().Includes(player) || CustomTeam.Get<ClassDTeam>().Includes(player)));
        int foundation = Player.ReadyList.Count(player =>
            IsCountable(player) &&
            (CustomTeam.Get<FoundationForcesTeam>().Includes(player) ||
             CustomTeam.Get<ScientistsTeam>().Includes(player) ||
             CustomTeam.Get<GuardsTeam>().Includes(player)));
        return hostile < foundation;
    }

    private static bool IsCountable(Player player) =>
        player is { IsDestroyed: false, IsPlayer: true, IsReady: true, IsAlive: true } &&
        player.Role is not RoleTypeId.None and not RoleTypeId.Spectator and not RoleTypeId.Overwatch;

    private void RegisterOverrides()
    {
        if (overrideRegistration != Guid.Empty || HasSpawned ||
            SpawnContext.Active is not DefaultContext)
            return;

        overrideRegistration = SpawnSystem.AddNextSpawnOverrides(
        [
            new SpawnSystem.NextSpawnOverride(SpawnContext.Wave<SecurityTeamWave>())
            {
                SourceSpawnableFaction = SpawnableFaction.NtfWave,
                Priority = 1000,
            },
            new SpawnSystem.NextSpawnOverride(SpawnContext.Wave<ChaosAgentsWave>())
            {
                SourceSpawnableFaction = SpawnableFaction.ChaosWave,
                Priority = 1000,
            },
        ], nameof(FirstWaveScheduler));
    }

    private void OnWaveSpawned(object sender, SpawnSystem.CustomSpawningEventArgs ev)
    {
        if (ev.Wave is not (SecurityTeamWave or ChaosAgentsWave)) return;

        HasSpawned = true;
        requested = false;
        Timing.KillCoroutines(timer);
        timer = default;
        CancelOverrides();
    }

    private void Reset()
    {
        Timing.KillCoroutines(timer);
        timer = default;
        ElapsedSeconds = 0f;
        HasSpawned = false;
        requested = false;
        WaitSeconds = DefaultWaitSeconds;
        CancelOverrides();
    }

    private void CancelOverrides()
    {
        if (overrideRegistration == Guid.Empty) return;
        SpawnSystem.RemoveNextSpawnOverride(overrideRegistration, nameof(FirstWaveScheduler));
        overrideRegistration = Guid.Empty;
    }
}
