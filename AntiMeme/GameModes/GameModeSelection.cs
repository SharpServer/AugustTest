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
using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.GameModes;

/// <summary>
/// ゲームモードのキューと直近履歴です。RA コマンド (<c>ModeCommand</c> / <c>StatusCommand</c>) が
/// この API 経由で次ラウンドを指定します。抽選は直近 3 回を避け、候補が尽きた時だけ全候補へ戻します。
/// </summary>
public static class GameModeSelection
{
    private const int RecentLimit = 3;

    /// <summary>
    /// 予約が無いラウンドで「モード無し」になる確率です。
    /// 旧 <c>SpecialEventsHandler.chance = 1f / 2f</c> と同じ 1/2。
    /// これが無いと毎ラウンド必ずどれかのモードが走ります。
    /// </summary>
    private const double NoModeChance = 0.5d;

    /// <summary>モードが走っても予約もされていないときの表示です。</summary>
    private const string NoModeName = "無し";

    private static readonly List<Type> Queue = new List<Type>();
    private static readonly List<Type> History = new List<Type>();

    // 予約先頭の表示名。看板は 0.05 秒ごとに読むので、
    // 表示のたびに GameMode を生成し直さないよう予約が動いたときだけ作り直す。
    private static string pendingName = NoModeName;

    // new Random() は Environment.TickCount 由来なので、同一 tick 内で複数回作ると同じ列になる。
    private static readonly SystemRandom Shared = new SystemRandom();

    public static IReadOnlyCollection<Type> Pending => Queue.ToArray();
    public static IReadOnlyList<Type> Recent => History;

    /// <summary>
    /// ロビーの看板と HUD の [Event] 欄に出す名前です。
    /// <b>走っているモードの表示名 → 予約先頭の表示名 → 「無し」</b> の順で、
    /// 旧 <c>SpecialEventsHandler.LocalizedEventName</c> と同じ決め方です。
    /// </summary>
    /// <remarks>
    /// 型名 (<c>FacilityTermination</c>) ではなく表示名 (<c>FACILITY TERMINATION</c>) を返します。
    /// </remarks>
    public static string DisplayName => GameMode.Current is { } running ? running.Name : pendingName;

    public static void Enqueue<T>() where T : GameMode, new() => Enqueue(typeof(T));

    public static bool Enqueue(Type modeType)
    {
        if (modeType is null || !typeof(GameMode).IsAssignableFrom(modeType) || modeType.IsAbstract)
            return false;

        Queue.Add(modeType);
        RefreshPendingName();

        return true;
    }

    public static void ClearQueue()
    {
        Queue.Clear();
        RefreshPendingName();
    }

    /// <summary>予約先頭の表示名を取り直します。予約を触ったら必ず呼びます。</summary>
    private static void RefreshPendingName()
    {
        pendingName = Queue.Count > 0 && TryCreate(Queue[0], out GameMode queued)
            ? queued.Name
            : NoModeName;
    }

    public static void ClearHistory() => History.Clear();

    /// <summary>
    /// ラウンド開始時の抽選です。予約が無ければ <see cref="NoModeChance"/> の確率で
    /// 「モード無し」を返します。管理者が明示的に振る <c>am mode roll</c> は
    /// <see cref="Next"/> を直に呼ぶので、この確率を通りません。
    /// </summary>
    public static GameMode NextForRound(SystemRandom random = null)
    {
        if (Queue.Count == 0 && (random ?? Shared).NextDouble() < NoModeChance)
        {
            Logger.Info("[AntiMeme] ゲームモード抽選: 今ラウンドはモード無し。");

            return null;
        }

        return Next(random);
    }

    public static GameMode Next(SystemRandom random = null)
    {
        while (Queue.Count > 0)
        {
            Type requested = Queue[0];
            Queue.RemoveAt(0);
            RefreshPendingName();

            if (TryCreate(requested, out GameMode queued) && queued.IsAvailable)
            {
                Record(requested);
                return queued;
            }
        }

        List<GameMode> candidates = GameMode.All()
            .Where(mode => mode.Weight > 0 && mode.IsAvailable)
            .ToList();
        if (candidates.Count == 0)
        {
            Logger.Info("[AntiMeme] ゲームモード抽選: 起動できる候補がありません。");

            return null;
        }

        // 旧 GetAllowedEvents と同じで、直近 RecentLimit 回に出たものは候補から外す。
        // 全部が直近だった場合は「モード無し」。ここで全候補へ戻すと連続で同じものが出る。
        candidates = candidates
            .Where(mode => !History.Contains(mode.GetType()))
            .ToList();
        if (candidates.Count == 0)
        {
            Logger.Info("[AntiMeme] ゲームモード抽選: 候補が全て直近 3 回に出ています。モード無し。");

            return null;
        }

        int total = candidates.Sum(mode => mode.Weight);
        int roll = (random ?? Shared).Next(Math.Max(1, total));

        Logger.Info(
            $"[AntiMeme] ゲームモード抽選: roll {roll}/{total} · 候補 " +
            string.Join(", ", candidates.Select(mode => $"{mode.GetType().Name}({mode.Weight})")));

        foreach (GameMode candidate in candidates)
        {
            roll -= candidate.Weight;
            if (roll < 0)
            {
                Record(candidate.GetType());
                Logger.Info($"[AntiMeme] ゲームモード抽選: {candidate.GetType().Name} を選びました。");
                return candidate;
            }
        }

        GameMode fallback = candidates[candidates.Count - 1];
        Record(fallback.GetType());
        Logger.Info($"[AntiMeme] ゲームモード抽選: {fallback.GetType().Name} を選びました (端数)。");
        return fallback;
    }

    private static bool TryCreate(Type type, out GameMode mode)
    {
        mode = null;
        try
        {
            mode = (GameMode)Activator.CreateInstance(type);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void Record(Type type)
    {
        History.Add(type);
        if (History.Count > RecentLimit)
            History.RemoveRange(0, History.Count - RecentLimit);
    }
}
