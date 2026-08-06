using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;

namespace AntiMeme.Maps.Features;

/// <summary>SCP-012/033 のローカルな接触通知です。能力層は event を購読します。</summary>
public sealed class Scp012_033 : MapFeature
{
    public static event Action<Player> Exposed;
    public override void RegisterEvents() => PlayerEvents.InteractingDoor += OnDoor;
    public override void UnregisterEvents() => PlayerEvents.InteractingDoor -= OnDoor;
    private static void OnDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (ev?.Player is not { IsDestroyed: false }) return;
        Exposed?.Invoke(ev.Player);
    }
}
