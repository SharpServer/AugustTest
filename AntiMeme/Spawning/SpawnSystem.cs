using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.Events.EventArgs.Server;
using Exiled.API.Enums;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Respawning.Waves;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using ExiledServer = Exiled.Events.Handlers.Server;
using Logger = LabApi.Features.Console.Logger;
using LabPlayer = LabApi.Features.Wrappers.Player;
using AntiMeme.Spawning.Waves;

namespace AntiMeme.Spawning;

/// <summary>
/// Intercepts the vanilla respawn event and assigns the selected unit-pack
/// roles itself. This keeps custom roles in the normal type-based lifecycle.
/// </summary>
public sealed class SpawnSystem : EventHandlerBase
{
    private const float DefaultWaveGateResetSeconds = 0.5f;

    private static readonly List<RegisteredOverride> PendingOverrides = [];
    private static long overrideSequence;
    private static bool disabled;
    private static bool defaultWaveGateOpen = true;
    private static CoroutineHandle gateResetHandle;
    private static bool spawning;

    /// <summary>Configuration used by the default wave selector.</summary>
    public static SpawnConfig Config { get; } = new SpawnConfig();

    /// <summary>Disables automatic and queued waves until the next reset.</summary>
    public static new bool Disable
    {
        get => disabled;
        set => disabled = value;
    }

    public static SpawnSystem Instance { get; private set; }

    public static event EventHandler<CustomSpawningEventArgs> Spawning;
    public static event EventHandler<CustomSpawningEventArgs> Spawned;

    /// <summary>Injectable index selection for deterministic tests.</summary>
    public static Func<int, int> IndexSelector { get; set; } = max => UnityEngine.Random.Range(0, max);

    public static SpawnOverrideMode OverrideMode => PendingOverrides.Count == 0
        ? SpawnOverrideMode.None
        : SpawnOverrideMode.NextWave;

    public static WaveSet PendingOverrideWave => PendingOverrides
        .OrderByDescending(candidate => candidate.Rule.Priority)
        .ThenBy(candidate => candidate.Sequence)
        .Select(candidate => candidate.Rule.Wave)
        .FirstOrDefault();

    public static bool PendingMiniWave => PendingOverrides
        .OrderByDescending(candidate => candidate.Rule.Priority)
        .ThenBy(candidate => candidate.Sequence)
        .Select(candidate => candidate.Rule.OverrideMiniWave)
        .FirstOrDefault() ?? false;

    public static int PendingOverrideCount => PendingOverrides.Count;

    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    public override void RegisterEvents()
    {
        Instance = this;
        ExiledServer.RespawningTeam += OnRespawningTeam;
        ExiledServer.RestartingRound += OnRoundReset;
        ExiledServer.WaitingForPlayers += OnRoundReset;
    }

    public override void UnregisterEvents()
    {
        ExiledServer.RespawningTeam -= OnRespawningTeam;
        ExiledServer.RestartingRound -= OnRoundReset;
        ExiledServer.WaitingForPlayers -= OnRoundReset;

        if (ReferenceEquals(Instance, this))
            Instance = null;

        ResetRuntimeState();
        Spawning = null;
        Spawned = null;
    }

    protected override void OnDisabled() => ResetRuntimeState();

    public enum SpawnOverrideMode
    {
        None,
        NextWave,
        Immediate,
    }

    /// <summary>
    /// ウェーブ抽選の閾値です。重みと比率は波 (<see cref="WaveSet"/>) と
    /// 文脈 (<see cref="SpawnContext"/>) が持つので、ここには残っていません。
    /// </summary>
    public sealed class SpawnConfig
    {
        public int ScpThresholdHigh { get; set; } = 3;
        public int PlayerThresholdHigh { get; set; } = 6;
        public bool NatoCallsign { get; set; } = true;
    }

    public sealed class CustomSpawningEventArgs : EventArgs
    {
        public CustomSpawningEventArgs(
            RespawningTeamEventArgs sourceEventArgs,
            SpawnContext nowContext,
            Faction faction,
            bool isMiniWave)
        {
            SourceEventArgs = sourceEventArgs;
            NowContext = nowContext;
            Faction = faction;
            IsMiniWave = isMiniWave;
        }

        public bool IsAllowed { get; set; } = true;
        public SpawnContext NowContext { get; }

        /// <summary>湧かせる波。購読側で差し替えられます。</summary>
        public WaveSet Wave { get; set; }
        public Faction Faction { get; }
        public bool IsMiniWave { get; }
        public RespawningTeamEventArgs SourceEventArgs { get; }
        public int SpawnCount { get; set; }
        public string CassieCallsign { get; set; } = string.Empty;
        public string DisplayCallsign { get; set; } = string.Empty;
    }

    public sealed class NextSpawnOverride
    {
        public NextSpawnOverride(WaveSet wave) => Wave = wave;

        public WaveSet Wave { get; }
        public SpawnableFaction? SourceSpawnableFaction { get; set; }
        public Faction? SourceFaction { get; set; }
        public bool? SourceIsMiniWave { get; set; }
        public bool? OverrideMiniWave { get; set; }
        public Func<RespawningTeamEventArgs, bool> Predicate { get; set; }
        public int Priority { get; set; }

        internal bool Matches(RespawningTeamEventArgs ev)
        {
            if (SourceSpawnableFaction.HasValue && ev.Wave.SpawnableFaction != SourceSpawnableFaction.Value)
                return false;
            if (SourceFaction.HasValue && ev.NextKnownTeam != SourceFaction.Value)
                return false;
            if (SourceIsMiniWave.HasValue && ev.Wave.IsMiniWave != SourceIsMiniWave.Value)
                return false;
            return Predicate is null || Predicate(ev);
        }

        internal NextSpawnOverride Snapshot() => new NextSpawnOverride(Wave)
        {
            SourceSpawnableFaction = SourceSpawnableFaction,
            SourceFaction = SourceFaction,
            SourceIsMiniWave = SourceIsMiniWave,
            OverrideMiniWave = OverrideMiniWave,
            Predicate = Predicate,
            Priority = Priority,
        };
    }

    private sealed class RegisteredOverride
    {
        public RegisteredOverride(Guid id, NextSpawnOverride rule, string source, long sequence)
        {
            Id = id;
            Rule = rule;
            Source = source;
            Sequence = sequence;
        }

        public Guid Id { get; }
        public NextSpawnOverride Rule { get; }
        public string Source { get; }
        public long Sequence { get; }
    }

    public static void ReplaceNextSpawn(WaveSet wave, bool? isMiniWave = null, string source = null)
    {
        ClearNextSpawnOverrides("replaced");
        AddNextSpawnOverride(new NextSpawnOverride(wave) { OverrideMiniWave = isMiniWave }, source);
    }

    public static Guid AddNextSpawnOverride(NextSpawnOverride rule, string source = null)
    {
        if (rule is null) throw new ArgumentNullException(nameof(rule));
        return AddNextSpawnOverrides(new[] { rule }, source);
    }

    public static Guid AddNextSpawnOverride(
        SpawnableFaction sourceFaction,
        WaveSet wave,
        bool? overrideMiniWave = null,
        string source = null,
        int priority = 0) => AddNextSpawnOverride(new NextSpawnOverride(wave)
        {
            SourceSpawnableFaction = sourceFaction,
            OverrideMiniWave = overrideMiniWave,
            Priority = priority,
        }, source);

    public static Guid AddNextSpawnOverride(
        Faction sourceFaction,
        WaveSet wave,
        bool? sourceIsMiniWave = null,
        bool? overrideMiniWave = null,
        string source = null,
        int priority = 0) => AddNextSpawnOverride(new NextSpawnOverride(wave)
        {
            SourceFaction = sourceFaction,
            SourceIsMiniWave = sourceIsMiniWave,
            OverrideMiniWave = overrideMiniWave,
            Priority = priority,
        }, source);

    public static Guid AddNextSpawnOverrides(IEnumerable<NextSpawnOverride> rules, string source = null)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));
        List<NextSpawnOverride> list = rules.Where(rule => rule is not null).ToList();
        if (list.Count == 0) throw new ArgumentException("At least one override rule is required.", nameof(rules));

        Guid id = Guid.NewGuid();
        string normalizedSource = string.IsNullOrWhiteSpace(source) ? "external" : source;
        foreach (NextSpawnOverride rule in list)
            PendingOverrides.Add(new RegisteredOverride(id, rule.Snapshot(), normalizedSource, overrideSequence++));

        return id;
    }

    public static bool RemoveNextSpawnOverride(Guid registrationId, string reason = "manual") =>
        PendingOverrides.RemoveAll(entry => entry.Id == registrationId) > 0;

    public static void ClearNextSpawnOverrides(string reason = "manual")
    {
        PendingOverrides.Clear();
        overrideSequence = 0;
    }

    public static void ForceSpawnNow(WaveSet wave, bool isMiniWave = false)
    {
        ClearNextSpawnOverrides("immediate spawn");
        Instance?.SummonForces(wave, isMiniWave);
    }

    public void SummonForces(WaveSet wave, bool isMiniWave)
    {
        if (spawning || disabled) return;
        spawning = true;
        CloseDefaultWaveGate();

        try
        {
            List<LabPlayer> candidates = LabPlayer.ReadyList
                .Where(IsWaveSpawnCandidate)
                .ToList();
            int spawnCount = wave.SpawnFor(candidates);
            CustomSpawningEventArgs args = new CustomSpawningEventArgs(
                null,
                SpawnContext.Active,
                wave.Faction,
                isMiniWave)
            {
                Wave = wave,
                SpawnCount = spawnCount,
            };
            FillCallsign(args);
            Spawned?.Invoke(this, args);
        }
        catch (Exception exception)
        {
            Logger.Error($"[AntiMeme] ウェーブ {wave?.Name} のスポーンに失敗しました: {exception}");
        }
        finally
        {
            spawning = false;
            ScheduleDefaultWaveGateReopen();
        }
    }

    private void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        ev.IsAllowed = false;
        if (disabled || spawning)
            return;

        RegisteredOverride pending = PendingOverrides
            .OrderByDescending(candidate => candidate.Rule.Priority)
            .ThenBy(candidate => candidate.Sequence)
            .FirstOrDefault(candidate => Matches(candidate, ev));

        if (pending is not null)
        {
            bool mini = pending.Rule.OverrideMiniWave ?? ev.Wave.IsMiniWave;
            PendingOverrides.RemoveAll(entry => entry.Id == pending.Id);
            SummonForces(pending.Rule.Wave, mini, ev);
            return;
        }

        if (!defaultWaveGateOpen)
            return;

        Faction faction = ev.NextKnownTeam;
        if (faction is not (Faction.FoundationStaff or Faction.FoundationEnemy))
            return;

        SpawnContext context = SpawnContext.Active;
        bool miniWave = ev.Wave.IsMiniWave;

        CustomSpawningEventArgs args = new CustomSpawningEventArgs(ev, context, faction, miniWave);
        Spawning?.Invoke(this, args);
        if (!args.IsAllowed) return;

        // 高脅威時は Nu-7 が出やすくなる。重みは文脈が持っているので、ここでは選ぶだけ。
        WaveSet selected = args.Wave
            ?? (faction == Faction.FoundationStaff && IsHighThreat() ? PickHighThreat(context, miniWave) : null)
            ?? context.Roll(faction, miniWave, () => ChooseIndex(TotalWeight(context, faction, miniWave)));

        if (selected is null) return;
        SummonForces(selected, miniWave, ev, args);
    }

    private void SummonForces(WaveSet wave, bool miniWave, RespawningTeamEventArgs source, CustomSpawningEventArgs args = null)
    {
        if (spawning || disabled) return;
        spawning = true;
        CloseDefaultWaveGate();

        try
        {
            List<LabPlayer> candidates = source.Players
                .Select(player => LabPlayer.Get(player.ReferenceHub))
                .Where(IsWaveSpawnCandidate)
                .ToList();
            int spawnCount = wave.SpawnFor(candidates);
            args ??= new CustomSpawningEventArgs(source, SpawnContext.Active, wave.Faction, miniWave);
            args.Wave = wave;
            args.SpawnCount = spawnCount;
            FillCallsign(args);
            Spawned?.Invoke(this, args);
        }
        catch (Exception exception)
        {
            Logger.Error($"[AntiMeme] ウェーブ {wave?.Name} のスポーンに失敗しました: {exception}");
        }
        finally
        {
            spawning = false;
            ScheduleDefaultWaveGateReopen();
        }
    }

    private static bool Matches(RegisteredOverride candidate, RespawningTeamEventArgs ev)
    {
        try { return candidate.Rule.Matches(ev); }
        catch (Exception exception)
        {
            Logger.Error($"[AntiMeme] Wave override ({candidate.Source}) の判定に失敗しました: {exception}");
            return false;
        }
    }

    private static bool IsHighThreat()
    {
        int players = LabPlayer.ReadyList.Count(player => player.IsPlayer);
        int scps = LabPlayer.ReadyList.Count(player => player.IsPlayer && player.RoleBase.Team == Team.SCPs);
        return players >= Config.PlayerThresholdHigh || scps >= Config.ScpThresholdHigh;
    }

    private static bool IsWaveSpawnCandidate(LabPlayer player) =>
        player is { IsDestroyed: false, IsPlayer: true, IsReady: true, Role: RoleTypeId.Spectator } &&
        CustomRole.Of(player) is null;

    /// <summary>
    /// 高脅威時に出す波です。既定では Nu-7。
    /// </summary>
    private static WaveSet PickHighThreat(SpawnContext context, bool miniWave)
    {
        WaveSet wave = SpawnContext.AllWaves
            .FirstOrDefault(candidate => candidate.IsMiniWave == miniWave && candidate is HdWave or HdBackupWave);

        return wave is not null && context.WeightOf(wave) > 0 ? wave : null;
    }

    private static int TotalWeight(SpawnContext context, Faction faction, bool miniWave) =>
        SpawnContext.AllWaves
            .Where(wave => wave.Faction == faction && wave.IsMiniWave == miniWave)
            .Sum(context.WeightOf);

    private static void Shuffle<T>(IList<T> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int other = ChooseIndex(index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }

    private static int ChooseIndex(int max)
    {
        if (max <= 1) return 0;
        int selected = IndexSelector?.Invoke(max) ?? 0;
        return Math.Max(0, Math.Min(max - 1, selected));
    }


    private static void FillCallsign(CustomSpawningEventArgs args)
    {
        if (!Config.NatoCallsign || args.Faction != Faction.FoundationStaff) return;
        int index = ChooseIndex(26);
        string[] cassie =
        ["NATO_A", "NATO_B", "NATO_C", "NATO_D", "NATO_E", "NATO_F", "NATO_G", "NATO_H", "NATO_I", "NATO_J",
         "NATO_K", "NATO_L", "NATO_M", "NATO_N", "NATO_O", "NATO_P", "NATO_Q", "NATO_R", "NATO_S", "NATO_T",
         "NATO_U", "NATO_V", "NATO_W", "NATO_X", "NATO_Y", "NATO_Z"];
        string[] display =
        ["ALPHA", "BRAVO", "CHARLIE", "DELTA", "ECHO", "FOXTROT", "GOLF", "HOTEL", "INDIA", "JULIETT",
         "KILO", "LIMA", "MIKE", "NOVEMBER", "OSCAR", "PAPA", "QUEBEC", "ROMEO", "SIERRA", "TANGO",
         "UNIFORM", "VICTOR", "WHISKEY", "XRAY", "YANKEE", "ZULU"];
        args.CassieCallsign = cassie[index];
        args.DisplayCallsign = $"{display[index]}-{ChooseIndex(19) + 1:00}";
    }

    private static void CloseDefaultWaveGate()
    {
        defaultWaveGateOpen = false;
        Timing.KillCoroutines(gateResetHandle);
    }

    private static void ScheduleDefaultWaveGateReopen()
    {
        gateResetHandle = Timing.CallDelayed(DefaultWaveGateResetSeconds, () => defaultWaveGateOpen = true);
    }

    private static void OnRoundReset() => ResetRuntimeState();

    private static void ResetRuntimeState()
    {
        Timing.KillCoroutines(gateResetHandle);
        gateResetHandle = default;
        defaultWaveGateOpen = true;
        spawning = false;
        disabled = false;
        ClearNextSpawnOverrides("round reset");
        SpawnContext.Reset();
    }
}
