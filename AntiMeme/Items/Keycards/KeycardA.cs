using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Sliced.API.Features;

using KeycardItem = LabApi.Features.Wrappers.KeycardItem;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 「認証キー - 赤」です。カードの形をしていますが、扉は一切開けません。
/// </summary>
/// <remarks>
/// 旧実装は地表アクセスパスの <c>Permissions</c> を <c>None</c> に書き換えていましたが、
/// 現行のバニラカードは権限を Detail に焼き付けており差し替えられません。
/// 代わりに、権限が要る扉への操作を持っている間だけ止めます。
/// </remarks>
public sealed class KeycardA : CustomItem
{
    private static bool hooked;

    public KeycardA() => Hook();

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SurfaceAccessPass;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;

    /// <inheritdoc/>
    protected override string PickupModel => "Alienisolation_keycard";

    /// <inheritdoc/>
    public override string Name => "認証キー - 赤";

    /// <inheritdoc/>
    public override string Description => "???";

    /// <inheritdoc/>
    protected override void Customize(LabApi.Features.Wrappers.Item item)
    {
        // 投げつけて扉を開ける経路も塞ぐ。
        if (item is KeycardItem keycard)
            keycard.Base.OpenDoorsOnThrow = false;
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.InteractingDoor += OnAnyInteractingDoor;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.InteractingDoor -= OnAnyInteractingDoor;
            hooked = false;
        });
    }

    private static void OnAnyInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (ev.Door.Permissions == DoorPermissionFlags.None) return;

        if (Of(ev.Player.CurrentItem) is KeycardA)
            ev.CanOpen = false;
    }
}
