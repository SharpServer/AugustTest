using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Hud;
using AntiMeme.Maps;
using AntiMeme.Maps.Features;
using AntiMeme.Maps.Features.Warhead;
using AntiMeme.Maps.Objects;
using AntiMeme.Roles;
using AntiMeme.Spawning;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using MEC;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using ExiledRoom = Exiled.API.Features.Room;
using SystemRandom = System.Random;
using AntiMeme.GameModes.Modes;

namespace AntiMeme.GameModes;

/// <summary>ラウンド開始時に Sliced のゲームモードを 1 つ選び、再開時に停止します。</summary>
public sealed class GameModeRoundHandler : EventHandlerBase
{
    private CoroutineHandle pending;

    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    public override void RegisterEvents()
    {
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        WarheadEvents.Starting += HandleWarheadStarting;
        ServerEvents.GeneratorActivated += HandleGeneratorActivated;
        PlayerEvents.DeactivatingGenerator += HandleGeneratorDeactivating;
        PlayerEvents.TriggeringTesla += HandleTriggeringTesla;
        ServerEvents.RoundEndingConditionsCheck += HandleFacilityProfile;
    }

    public override void UnregisterEvents()
    {
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        WarheadEvents.Starting -= HandleWarheadStarting;
        ServerEvents.GeneratorActivated -= HandleGeneratorActivated;
        PlayerEvents.DeactivatingGenerator -= HandleGeneratorDeactivating;
        PlayerEvents.TriggeringTesla -= HandleTriggeringTesla;
        ServerEvents.RoundEndingConditionsCheck -= HandleFacilityProfile;
        Timing.KillCoroutines(pending);
    }

    private void OnWaitingForPlayers()
    {
        Timing.KillCoroutines(pending);
        profileEndScheduled = false;
        GameMode.StopCurrent();
    }

    private void OnRoundStarted()
    {
        Timing.KillCoroutines(pending);
        profileEndScheduled = false;
        pending = RoundScope.Current.Delay(0.75f, StartNextMode);
    }

    private static void StartNextMode()
    {
        if (GameMode.Current is not null || !Round.IsRoundInProgress)
            return;

        GameModeSelection.NextForRound()?.Start();
    }

    private static void HandleWarheadStarting(WarheadStartingEventArgs ev)
    {
        if (GameMode.Current is { AllowsWarhead: false })
            ev.IsAllowed = false;
    }

    private static void HandleGeneratorActivated(GeneratorActivatedEventArgs ev)
    {
        if (GameMode.Current is CaseColourlessGreen mode)
            mode.OnGeneratorActivated();
    }

    private static void HandleGeneratorDeactivating(PlayerDeactivatingGeneratorEventArgs ev)
    {
        if (GameMode.Current is CaseColourlessGreen)
            ev.IsAllowed = false;
    }

    private static void HandleTriggeringTesla(PlayerTriggeringTeslaEventArgs ev)
    {
        // どのモードで止めるかはモード自身が名乗る。ここに型を並べない。
        if (GameMode.Current is RaidMode { AllowsTesla: false })
            ev.IsAllowed = false;
    }

    private static bool profileEndScheduled;

    internal static void ResetProfileState() => profileEndScheduled = false;

    private static void HandleFacilityProfile(RoundEndingConditionsCheckEventArgs ev)
    {
        if (GameMode.Current is not FacilityTermination || profileEndScheduled)
            return;

        // FacilityTermination は旧 CTeamProfileDefinition の Humanity/Normalcy を実行時に
        // 評価します。第五教会・イニシアチブ等の Undefined 陣営はどちらにも数えません。
        int humanity = Player.ReadyList.Count(player => player is { IsDestroyed: false, IsAlive: true } && IsFacilityHumanity(player));
        int normalcy = Player.ReadyList.Count(player => player is { IsDestroyed: false, IsAlive: true } && IsFacilityNormalcy(player));
        if (humanity > 0 && normalcy > 0)
        {
            ev.CanEnd = false;
            return;
        }

        if (humanity == 0 && normalcy == 0)
            return;

        ev.CanEnd = false;
        profileEndScheduled = true;
        foreach (Player player in Player.ReadyList)
            player.SendHint(humanity > 0 ? "<color=#0000c8>HUMANITY WINS</color>" : "<color=#00b7eb>NORMALCY WINS</color>", 10f);
        RoundScope.Current.Delay(10f, () => Round.End(force: true));
    }

    /// <summary>FacilityTermination の人類陣営かどうか。HUD の表示もここを見ます。</summary>
    internal static bool IsFacilityHumanity(Player player) =>
        CustomTeam.Of(player) is ScientistsTeam or ClassDTeam or ChaosInsurgencyTeam
            or GoCTeam or OthersTeam or WarriorsTeam;

    /// <summary>FacilityTermination の正常性陣営かどうか。</summary>
    internal static bool IsFacilityNormalcy(Player player) =>
        CustomTeam.Of(player) is FoundationForcesTeam or GuardsTeam or ScpTeam;
}
