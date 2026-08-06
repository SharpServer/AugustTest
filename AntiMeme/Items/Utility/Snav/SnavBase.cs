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

/// <summary>投擲で探知を行う S-Nav の共通基底です。</summary>
public abstract class SnavBase : CustomItem
{
    private static bool hooked;
    private RadioMessages.RadioRangeLevel mode = RadioMessages.RadioRangeLevel.LowRange;

    protected SnavBase() => Hook();

    protected abstract IReadOnlyCollection<RoomName> Targets { get; }
    protected virtual bool UsesBattery => true;
    protected virtual bool DetectsScps => false;

    public override ItemType BaseType => ItemType.Radio;

    protected override void Customize(Item item)
    {
        if (item is LabRadioItem radio)
            radio.BatteryPercent = UsesBattery ? radio.BatteryPercent : (byte)100;
    }

    protected override void OnDropped(Player player)
    {
        // Drop is intercepted below; this callback is intentionally only for normal drops.
    }

    private void ChangeRange(PlayerChangingRadioRangeEventArgs ev)
    {
        if (Of(ev.RadioItem.Serial) is not SnavBase nav)
            return;

        nav.mode = ev.Range;
        ev.Player.SendHint(SnavCommon.RangeHint(ev.Range), 2f);
    }

    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw || Of(ev.Item.Serial) is not SnavBase nav)
            return;

        ev.IsAllowed = false;
        if (ev.Item is not LabRadioItem radio)
            return;

        float cost = SnavCommon.Consumption(nav.mode);
        if (nav.UsesBattery && radio.BatteryPercent < cost)
        {
            ev.Player.SendHint("バッテリー不足！", 2f);
            return;
        }

        if (nav.UsesBattery)
            radio.BatteryPercent = (byte)Mathf.Clamp(radio.BatteryPercent - Mathf.RoundToInt(cost), 0, 100);

        Vector3 origin = ev.Player.Position;
        List<Room> rooms = SnavCommon.DetectRooms(origin, nav.mode, nav.Targets);
        string result = SnavCommon.RoomsHint(nav.mode, rooms, origin);

        if (nav.DetectsScps)
        {
            float range = SnavCommon.Range(nav.mode);
            List<Player> scps = Player.List
                .Where(player => player is { IsAlive: true, IsSCP: true } &&
                                 Vector3.Distance(origin, player.Position) <= range)
                .OrderBy(player => Vector3.Distance(origin, player.Position))
                .ToList();
            string scpText = scps.Count == 0
                ? "なし"
                : string.Join("\n", scps.Select(player =>
                    $"{player.Nickname} ({player.Role}): {Vector3.Distance(origin, player.Position):F0}m"));
            result += $"\n\n検知されたSCP：\n{scpText}";
        }

        ev.Player.SendHint(result, 10f);
    }

    private static void Hook()
    {
        if (hooked)
            return;

        hooked = true;
        PlayerEvents.ChangingRadioRange += OnChangingRadioRange;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.ChangingRadioRange -= OnChangingRadioRange;
            hooked = false;
        });
    }

    private static void OnChangingRadioRange(PlayerChangingRadioRangeEventArgs ev)
    {
        if (Of(ev.RadioItem.Serial) is SnavBase nav)
            nav.ChangeRange(ev);
    }

}
