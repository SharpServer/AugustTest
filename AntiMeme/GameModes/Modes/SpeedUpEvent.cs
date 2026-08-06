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

namespace AntiMeme.GameModes.Modes;

public sealed class SpeedUpEvent : RaidMode
{
    public override string Name => "- ULTIMATE SPEED EX -";
    public override string Description => "全職員に超絶加速アドレナリンが注入される。";
    public override int MinimumPlayers => 0;
    public override int Weight => 6;

    /// <inheritdoc/>
    protected override IReadOnlyList<Season> Seasons => [Season.April];

    protected override void OnStarted()
    {
        ApplySpeed();
        Say("Extremely Advanced Adrenaline Injection Detected", "全職員に超絶加速アドレナリンが注入されました。", true);
        Loop(5f, ApplySpeed);
    }

    private static void ApplySpeed()
    {
        foreach (Player player in LivingPlayers())
        {
            player.EnableEffect<MovementBoost>(255);
            player.EnableEffect<Scp207>(255);
        }
    }

    private static IEnumerable<Player> LivingPlayers() => Player.ReadyList.Where(IsLiving);

    protected override void OnStopped()
    {
        foreach (Player player in Player.ReadyList)
        {
            player.DisableEffect<MovementBoost>();
            player.DisableEffect<Scp207>();
        }

        base.OnStopped();
    }
}
