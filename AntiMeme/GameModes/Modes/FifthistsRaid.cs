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
using AntiMeme.Roles.Fifthist;

namespace AntiMeme.GameModes.Modes;

public sealed class FifthistsRaid : RaidMode
{
    public override string Name => "Fifthists Raid";
    public override string Description => "第五教会が施設へ侵入し、SCP-3005を導く。";
    public override int MinimumPlayers => 4;
    public override int Weight => 10;

    protected override void OnStarted()
    {
        Delay(.8f, () =>
        {
            List<Player> humans = LivingHumans().OrderBy(_ => UnityEngine.Random.value).ToList();
            foreach (Player player in humans.Take(Mathf.Max(1, Player.ReadyList.Count() / 4)))
                CustomRole.Spawn<FifthistRescure>(player);
            if (!Living.Any(CustomRole.Is<Scp3005>) && LivingScps().FirstOrDefault() is { } scp)
                CustomRole.Spawn<Scp3005>(scp);
        });
        Delay(8f, () =>
        {
            MapAudio.Play("WaveThemes/_w_fifthists.ogg", "WaveTheme", Vector3.zero, maxDistance: 999f);
            Say("5 5 5 Forces detected", "<color=#ff00fa>第五教会勢力を検知しました。</color>", true);
        });
    }
}
