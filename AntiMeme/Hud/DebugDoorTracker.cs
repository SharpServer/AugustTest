using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

using AntiMeme.Maps;

namespace AntiMeme.Hud;

/// <summary>
/// デバッグ HUD 用に「最後に触ったドア」を覚えます。
/// マップにドア基準で物を置くとき、ルームローカル座標と回転がここに出ます。
/// </summary>
public sealed class DebugDoorTracker : EventHandlerBase
{
    /// <inheritdoc/>
    public override void RegisterEvents() => PlayerEvents.InteractedDoor += OnInteractedDoor;

    /// <inheritdoc/>
    public override void UnregisterEvents() => PlayerEvents.InteractedDoor -= OnInteractedDoor;

    private static void OnInteractedDoor(PlayerInteractedDoorEventArgs ev)
    {
        if (ev.Player is not { IsDestroyed: false } player || !DebugMode.IsOn(player)) return;
        if (ev.Door is not { } door) return;

        // Door は複数の部屋にまたがることがある。基準は最初の部屋で足りる。
        Room room = door.Rooms.FirstOrDefault();
        Quaternion inverse = room is null ? Quaternion.identity : Quaternion.Inverse(room.Rotation);
        Vector3 origin = room is null ? Vector3.zero : room.Position;

        DebugMode.Remember(player, new DebugMode.DoorSnapshot(
            string.IsNullOrEmpty(door.NameTag) ? door.DoorName.ToString() : door.NameTag,
            RoomNames.Of(room, "None"),
            inverse * (door.Position - origin),
            (inverse * door.Rotation).eulerAngles,
            room is null ? Vector3.zero : room.Rotation.eulerAngles));
    }
}
