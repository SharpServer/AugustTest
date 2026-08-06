using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using Sliced.API.Features;

namespace AntiMeme.Changes;

/// <summary>
/// SCP はチェックポイントを通れるようにします。
/// 施設側の扉に足止めされると収容違反として成立しないためです。
/// </summary>
public sealed class ScpDoorAccess : EventHandlerBase
{
    /// <inheritdoc/>
    public override void RegisterEvents() => PlayerEvents.InteractingDoor += OnInteractingDoor;

    /// <inheritdoc/>
    public override void UnregisterEvents() => PlayerEvents.InteractingDoor -= OnInteractingDoor;

    private static void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (CustomTeam.Of(ev.Player) is not ScpTeam) return;

        if (ev.Door.DoorName is DoorName.LczCheckpointA or DoorName.LczCheckpointB
            or DoorName.HczCheckpoint)
        {
            ev.IsAllowed = true;
        }
    }
}
