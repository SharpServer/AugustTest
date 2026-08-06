using AntiMeme.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using AntiMeme.Teams.Factions;
using InventorySystem.Items.Radio;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using Sliced.API.Features;
using UnityEngine;

using LabRadioItem = LabApi.Features.Wrappers.RadioItem;

namespace AntiMeme.Items.Utility.Snav;

/// <summary>共有する S-Nav のモード、範囲、部屋表示をまとめます。</summary>
internal static class SnavCommon
{
    public static float Range(RadioMessages.RadioRangeLevel level) => level switch
    {
        RadioMessages.RadioRangeLevel.LowRange => 30f,
        RadioMessages.RadioRangeLevel.MediumRange => 60f,
        RadioMessages.RadioRangeLevel.HighRange => 80f,
        RadioMessages.RadioRangeLevel.UltraRange => 100f,
        _ => 0f,
    };

    public static string RangeHint(RadioMessages.RadioRangeLevel level) => level switch
    {
        RadioMessages.RadioRangeLevel.LowRange => "近距離(30m)探知モード",
        RadioMessages.RadioRangeLevel.MediumRange => "中距離(60m)探知モード",
        RadioMessages.RadioRangeLevel.HighRange => "長距離(80m)探知モード",
        RadioMessages.RadioRangeLevel.UltraRange => "超長距離(100m)探知モード",
        _ => string.Empty,
    };

    public static float Consumption(RadioMessages.RadioRangeLevel level) => level switch
    {
        RadioMessages.RadioRangeLevel.LowRange => 5f,
        RadioMessages.RadioRangeLevel.MediumRange => 10f,
        RadioMessages.RadioRangeLevel.HighRange => 15f,
        RadioMessages.RadioRangeLevel.UltraRange => 25f,
        _ => 40f,
    };

    public static List<Room> DetectRooms(Vector3 origin, RadioMessages.RadioRangeLevel level,
        IReadOnlyCollection<RoomName> targets)
    {
        float range = Range(level);
        if (range <= 0f)
            return [];

        return Room.List
            .Where(room => room is not null && targets.Contains(room.Name) &&
                           Vector3.Distance(origin, room.Position) <= range)
            .OrderBy(room => Vector3.Distance(origin, room.Position))
            .ToList();
    }

    public static string RoomsHint(RadioMessages.RadioRangeLevel level, List<Room> detected, Vector3 origin) =>
        detected.Count == 0
            ? "検知された部屋なし"
            : $"[{level}]見つかった部屋：\n" + string.Join("\n", detected.Select(room =>
                $"{Translate(room)}: {Vector3.Distance(origin, room.Position):F0}m"));

    public static string Translate(Room room) => RoomNames.Of(room);
}
