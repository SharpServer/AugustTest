using AntiMeme.Maps.Objects;
using LabApi.Events.Arguments.WarheadEvents;
using Exiled.API.Enums;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using ExiledRoom = Exiled.API.Features.Room;

namespace AntiMeme.Changes;

/// <summary>
/// 施設の常設照明です。区画ごとに色を変え、核起動中は赤に振り切ります。
/// </summary>
/// <remarks>
/// ゲームモード側が一時的に色を塗り替えても、モードが終われば
/// <see cref="RaidMode.OnStopped"/> が白へ戻すだけなので、
/// 平常の色はここが受け持ちます。
/// </remarks>
public sealed class FacilityLight : EventHandlerBase
{
    private static readonly Color Surface = Parse("#c1eaff");
    private static readonly Color Facility = Parse("#9bddff");
    private static readonly Color LightContainment = Parse("#fcd4b0");
    private static readonly Color Intercom = Parse("#FFBCBC");
    private static readonly Color Alert = Parse("#FF0000");
    private static readonly Color Warhead = Parse("#ff1500");

    /// <inheritdoc/>
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <inheritdoc/>
    public override void OnServerWaitingForPlayers() => Normal();

    /// <inheritdoc/>
    public override void OnServerRoundStarted() => Normal();

    /// <inheritdoc/>
    public override void OnWarheadStarted(WarheadStartedEventArgs ev)
    {
        foreach (ExiledRoom room in ExiledRoom.List)
            room.Color = Warhead;

        AlarmLight.SetAlarmState(true);
    }

    /// <inheritdoc/>
    public override void OnWarheadStopped(WarheadStoppedEventArgs ev)
    {
        Normal();
        AlarmLight.SetAlarmState(false);
    }

    /// <summary>平常の配色に戻します。</summary>
    public static void Normal()
    {
        foreach (ExiledRoom room in ExiledRoom.List)
        {
            room.Color = room.Zone switch
            {
                ZoneType.Surface => Surface,
                ZoneType.Entrance => EntranceColor(room),
                ZoneType.HeavyContainment => Facility,
                ZoneType.LightContainment => LightContainmentColor(room),
                _ => room.Color,
            };
        }
    }

    private static Color EntranceColor(ExiledRoom room) => room.Type switch
    {
        RoomType.EzIntercom => Intercom,
        RoomType.EzVent or RoomType.EzShelter => Alert,
        _ => Facility,
    };

    private static Color LightContainmentColor(ExiledRoom room) =>
        room.Type is RoomType.LczAirlock ? Alert : LightContainment;

    private static Color Parse(string html) =>
        ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
}
