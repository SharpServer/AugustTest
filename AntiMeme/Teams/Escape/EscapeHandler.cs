using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps;
using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Teams.Escape;

/// <summary>
/// バニラ脱出イベントと ProjectMER の <c>EscapePoint</c> を同じ型付き規則へ流します。
/// </summary>
public sealed class EscapeHandler : EventHandlerBase
{
    private const float PollInterval = 0.5f;
    private const float EscapeRadius = 1.75f;
    private const float ReentryDelay = 5f;

    private readonly HashSet<Player> recent = [];
    private CoroutineHandle polling;

    public override HandlerLifetime Lifetime => HandlerLifetime.Round;

    public override void RegisterEvents() => PlayerEvents.Escaping += OnEscaping;

    public override void UnregisterEvents() => PlayerEvents.Escaping -= OnEscaping;

    protected override void OnEnabled() =>
        polling = Timing.RunCoroutine(PollCustomEscapePoints());

    protected override void OnDisabled()
    {
        Timing.KillCoroutines(polling);
        recent.Clear();
    }

    private void OnEscaping(PlayerEscapingEventArgs ev)
    {
        EscapeTarget target = EscapeRule.ResolveAll(ev.Player);

        if (target.IsEmpty)
        {
            ev.IsAllowed = false;
            return;
        }

        MarkRecent(ev.Player);

        if (target.Vanilla is { } vanilla)
        {
            List<Pickup> dropped = ev.Player.DropAllItems();
            ev.NewRole = vanilla;
            RoundScope.Current.Delay(0.1f, () => MoveDroppedItems(dropped, ev.Player.Position));
            return;
        }

        ev.IsAllowed = false;
        ApplyCustom(ev.Player, target.Custom);
    }

    private IEnumerator<float> PollCustomEscapePoints()
    {
        while (!IsDisposed)
        {
            yield return Timing.WaitForSeconds(PollInterval);

            var points = MapPoints.All("EscapePoint");
            if (points.Count == 0) continue;

            foreach (Player player in Player.ReadyList)
            {
                if (!player.IsAlive || recent.Contains(player)) continue;

                Vector3 position = player.Position;
                bool inside = points.Any(point =>
                    (position - point.Position).sqrMagnitude <= EscapeRadius * EscapeRadius);

                if (!inside) continue;

                EscapeTarget target = EscapeRule.ResolveAll(player);
                if (target.IsEmpty) continue;

                MarkRecent(player);

                if (target.Custom is not null)
                    ApplyCustom(player, target.Custom);
                else if (target.Vanilla is { } vanilla)
                    ApplyVanilla(player, vanilla);
            }
        }
    }

    private void MarkRecent(Player player)
    {
        recent.Add(player);
        RoundScope.Current.Delay(ReentryDelay, () => recent.Remove(player));
    }

    private static void ApplyCustom(Player player, Type roleType)
    {
        List<Pickup> dropped = player.DropAllItems();

        if (CustomRole.Spawn(roleType, player) is null) return;

        MoveDroppedItems(dropped, player.Position);
    }

    private static void ApplyVanilla(Player player, RoleTypeId role)
    {
        List<Pickup> dropped = player.DropAllItems();
        player.SetRole(role);
        MoveDroppedItems(dropped, player.Position);
    }

    private static void MoveDroppedItems(IEnumerable<Pickup> pickups, Vector3 position)
    {
        Vector3 destination = position + Vector3.up * 0.15f;

        foreach (Pickup pickup in pickups)
        {
            if (pickup is { IsSpawned: true })
                pickup.Position = destination;
        }
    }
}
