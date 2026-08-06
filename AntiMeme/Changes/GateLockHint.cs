using AntiMeme.GameModes;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using Sliced.API.Features;

namespace AntiMeme.Changes;

/// <summary>
/// 施錠されているゲート A / B に触れた人へ、なぜ開かないのかを伝えます。
/// </summary>
/// <remarks>
/// 旧実装では <c>EventHandler.DoorGet</c> の中に、デバッグ表示の記録と同居していました
/// (記録側は <see cref="Hud.DebugDoorTracker"/> にあります)。
/// ゲームモードが走っている間はモード側が意図してゲートを閉めているので黙ります。
/// </remarks>
public sealed class GateLockHint : EventHandlerBase
{
    /// <inheritdoc/>
    public override void RegisterEvents() => PlayerEvents.InteractingDoor += OnInteractingDoor;

    /// <inheritdoc/>
    public override void UnregisterEvents() => PlayerEvents.InteractingDoor -= OnInteractingDoor;

    private static void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (ev.Door is not { IsLocked: true } door) return;
        if (door.DoorName is not (DoorName.EzGateA or DoorName.EzGateB)) return;

        // モードが閉めているなら、そちらの演出が理由を語る。
        if (GameMode.Current is not null) return;

        ev.Player?.SendHint("収容違反への対応として暫くロックされているようだ・・・", 3f);
    }
}
