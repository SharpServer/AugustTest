using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Hud;
using AntiMeme.Maps;
using AntiMeme.Maps.Features;
using AntiMeme.Maps.Features.Warhead;
using AntiMeme.Maps.Objects;
using AntiMeme.Roles;
using AntiMeme.Roles.Scps;
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

public sealed class EndlessCry : RaidMode
{
    public override string Name => "ENDLESS CRY";
    public override string Description => "SCP-096の怒りが永遠に続く。";
    public override int MinimumPlayers => 4;
    public override int Weight => 7;

    protected override void OnStarted()
    {
        Delay(.5f, () =>
        {
            if (LivingScps().FirstOrDefault() is { } scp)
            {
                Say("SCP 0 9 6 Detected", "<color=red>SCP-096！SCP-096！</color>", true);
                CustomRole.Spawn<Scp096Anger>(scp);
            }
        });
    }
}
