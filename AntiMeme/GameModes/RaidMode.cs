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
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using MEC;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;
using Utils;

using ExiledRoom = Exiled.API.Features.Room;
using SystemRandom = System.Random;

namespace AntiMeme.GameModes;

/// <summary>共通のゲームモード処理。ラウンドスコープに遅延処理と後始末を預けます。</summary>
public abstract class RaidMode : GameMode
{
    /// <summary>
    /// このモードが走れる季節です。空なら通年。
    /// 旧実装の <c>IsReadyToExecute() =&gt; MapFlags.GetSeason() == ...</c> がここに相当します。
    /// これが無いと季節限定モードが年中抽選に残り、重みの大きいものが毎ラウンド当たります。
    /// </summary>
    protected virtual IReadOnlyList<Season> Seasons => [];

    /// <inheritdoc/>
    public override bool IsAvailable =>
        base.IsAvailable && (Seasons.Count == 0 || Seasons.Contains(AntiMemePlugin.Settings.Season));

    /// <summary>テスラゲートを働かせるか。施設を掌握する系のモードは false にします。</summary>
    public virtual bool AllowsTesla => true;

    /// <summary>LCZ の除染を進めるか。</summary>
    public virtual bool AllowsDecontamination => true;

    protected static IEnumerable<Player> Living => Player.ReadyList.Where(IsLiving);

    protected static bool IsLiving(Player player) => player is { IsDestroyed: false, IsAlive: true };

    protected static List<Player> LivingScps() => Living.Where(player => player.IsSCP).ToList();

    protected static List<Player> LivingHumans() => Living.Where(player => !player.IsSCP).ToList();

    protected static void Say(string cassie, string subtitle, bool noise = true) =>
        FacilityAnnouncer.Say(cassie, subtitle, noise);

    protected CoroutineHandle Delay(float seconds, Action action) => Scope.Delay(seconds, action);

    protected CoroutineHandle Loop(float interval, Action action) => Scope.RunLoop(interval, action);

    protected static void ConvertRatio<T>(IEnumerable<Player> source, float ratio) where T : CustomRole, new()
    {
        ConvertRatio(typeof(T), source, ratio);
    }

    protected static void ConvertRatio(Type roleType, IEnumerable<Player> source, float ratio)
    {
        List<Player> candidates = source.ToList();
        Player[] targets = candidates.OrderBy(_ => UnityEngine.Random.value)
            .Take(Mathf.Max(1, Mathf.CeilToInt(candidates.Count * ratio)))
            .ToArray();

        foreach (Player target in targets)
            CustomRole.Spawn(roleType, target);
    }

    protected static Player FirstOrDefault(IEnumerable<Player> players) => players.FirstOrDefault(IsLiving);

    protected static void OpenAndLockDoors()
    {
        foreach (Door door in Door.List)
        {
            if (door is not { IsDestroyed: false }) continue;
            door.IsOpened = true;
            door.IsLocked = true;
        }
    }

    protected static void OpenDoors()
    {
        foreach (Door door in Door.List)
        {
            if (door is not { IsDestroyed: false }) continue;
            door.IsLocked = false;
            door.IsOpened = true;
        }
    }

    protected static void TintRooms(Color color)
    {
        foreach (ExiledRoom room in ExiledRoom.List)
        {
            room.AreLightsOff = false;
            room.Color = color;
        }
    }

    protected static void KillLiving(string reason)
    {
        foreach (Player player in Living.ToArray())
            player.Damage(100000f, reason);
    }

    /// <summary>
    /// 生存者を爆散させます。地上と地下で死因が変わるモードのために理由を選べます。
    /// </summary>
    protected static void ExplodeLiving(string undergroundReason, string surfaceReason = null)
    {
        foreach (Player player in Living.ToArray())
        {
            ExplosionUtils.ServerSpawnEffect(player.Position, ItemType.GrenadeHE);

            bool surface = player.Room?.Zone == FacilityZone.Surface;
            player.Damage(100000f, surface ? surfaceReason ?? undergroundReason : undergroundReason);
        }
    }

    /// <summary>
    /// 部屋を 1 つずつ順に点けていきます。一斉に切り替えるより演出が効きます。
    /// </summary>
    protected void LightUp(Color color, float interval = 0.25f)
    {
        List<ExiledRoom> remaining = ExiledRoom.List.ToList();

        CoroutineHandle handle = default;

        handle = Loop(interval, () =>
        {
            if (remaining.Count == 0)
            {
                Timing.KillCoroutines(handle);

                return;
            }

            ExiledRoom room = remaining[0];
            remaining.RemoveAt(0);

            room.AreLightsOff = false;
            room.Color = color;
        });
    }

    /// <summary>
    /// エレベーター以外の扉をすべて開け、核ロックで固定します。
    /// </summary>
    protected static void OpenAndSealDoors()
    {
        foreach (Door door in Door.List)
        {
            if (door is not { IsDestroyed: false }) continue;
            if (door.Base is Interactables.Interobjects.ElevatorDoor) continue;

            door.IsOpened = true;
            door.Lock(DoorLockReason.Warhead, true);
        }
    }

    protected override void OnStopped()
    {
        GameModeRoundHandler.ResetProfileState();
        OpenDoors();
        foreach (ExiledRoom room in ExiledRoom.List)
        {
            room.AreLightsOff = false;
            room.Color = Color.white;
        }
    }
}
