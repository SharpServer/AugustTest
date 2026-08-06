using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>同じ LocalLevel のエレベーター間を移動する簡易リフトです。</summary>
public sealed class EzShelterElevator : ObjectPrefab
{
    public static bool IsTransitioning { get; private set; }
    public static int GlobalLevel { get; set; }
    public int LocalLevel { get; set; }
    public float RideDuration { get; set; } = 4f;
    public float TeleportDelay { get; set; } = 2.5f;
    protected override string SchematicName => "ALN_Lift";
    protected override void OnSetup()
    {
        if (GetInteractable("Interactable") is { } handle) handle.Interacted += ev => Ride(ev.Player);
    }
    public bool Ride(Player player)
    {
        if (IsTransitioning || player is not { IsDestroyed: false, IsAlive: true }) return false;
        EzShelterElevator destination = ObjectPrefab.All.OfType<EzShelterElevator>().FirstOrDefault(x => x != this && x.LocalLevel == LocalLevel);
        if (destination is null) return false;
        IsTransitioning = true;
        MapAudio.Play("ElevatorMoving.ogg", "EzShelterElevator", Position, maxDistance: 15f);
        Delay(Mathf.Max(0f, TeleportDelay), () =>
        {
            if (player is { IsDestroyed: false, IsAlive: true }) player.Position = destination.Position + Vector3.up;
            IsTransitioning = false;
        });
        return true;
    }
}
