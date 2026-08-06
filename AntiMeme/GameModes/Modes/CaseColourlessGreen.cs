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
using AntiMeme.Roles.FoundationForces;
using AntiMeme.Roles.Scientist;

namespace AntiMeme.GameModes.Modes;

public sealed class CaseColourlessGreen : RaidMode
{
    private int generatorsActivated;
    private AntiMemeBomb antiMemeBomb;

    public override string Name => "CASE COLOURLESS GREEN";

    /// <inheritdoc/>
    public override bool AllowsBreachAnnouncement => false;
    public override string Description => "反ミーム爆弾を起動するため、発電機を順に起動する。";
    public override int MinimumPlayers => 3;
    public override int Weight => 10;
    public override bool AllowsWarhead => false;

    /// <inheritdoc/>
    protected override IReadOnlyList<Season> Seasons => [Season.FifthFestival, Season.Summer];

    protected override void OnStarted()
    {
        OpenAndLockDoors();
        Vector3 bombPosition = SpawnPoints.InRoom(RoomName.LczClassDSpawn, new Vector3(-25.3f, 0f, 0f)) ?? Vector3.zero;
        antiMemeBomb = new AntiMemeBomb { Position = bombPosition };
        antiMemeBomb.Create();
        Delay(1.5f, () =>
        {
            List<Player> candidates = LivingHumans().OrderBy(_ => UnityEngine.Random.value).ToList();
            if (LivingScps().FirstOrDefault() is { } scp)
                CustomRole.Spawn<Scp3125>(scp);
            if (candidates.Count > 0)
                CustomRole.Spawn<AraOrun>(candidates[0]);
            foreach (Player player in candidates.Skip(1))
                CustomRole.Spawn(UnityEngine.Random.value < .2f ? typeof(FifthistMarionetteRole) : typeof(AntiMemeDivisionScientist), player);
        });
        Say("Attention All personnel. Anti Meme process in light containment zone", "全職員に通達。軽度収容区画の反ミーム爆弾を起動してください。", true);
        Loop(20f, () =>
        {
            if (!Living.Any(CustomRole.Is<Scp3125>))
            {
                Say("Anti Meme Forces were terminated", "反ミーム勢力は排除されました。施設は通常運用へ戻ります。", true);
                GameMode.StopCurrent();
                return;
            }

            foreach (Player player in Player.ReadyList.Where(player => player.Role is RoleTypeId.Spectator && CustomRole.Of(player) is null))
                CustomRole.Spawn<FifthistMarionetteRole>(player);
        });
    }

    internal void OnGeneratorActivated()
    {
        generatorsActivated++;
        Say("Generator activated", $"発電機を起動しました。進捗: {generatorsActivated}/3", false);
        if (generatorsActivated >= 3)
        {
            OpenDoors();
            Say("Anti Meme process ready", "反ミーム爆弾の起爆が可能になりました。", true);
        }
    }

    protected override void OnStopped()
    {
        antiMemeBomb?.Destroy();
        antiMemeBomb = null;
        base.OnStopped();
    }
}
