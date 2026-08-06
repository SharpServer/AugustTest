using System;
using AntiMeme.Items;
using AntiMeme.Items.Bases;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>ドア・発電機の作業を切り替えて行う工具箱です。</summary>
public sealed class Toolbox : CustomItem
{
    public enum UtilityMode { Work, MaintenanceLock }

    private static bool hooked;
    private UtilityMode mode;
    private float cooldownAt;

    public Toolbox() => Hook();

    public override ItemType BaseType => ItemType.Coin;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;

    /// <inheritdoc/>
    protected override string PickupModel => "ToolboxModel";
    public override string Name => "Toolbox";
    public override string Description => "投げる操作で機能を切り替える便利な工具箱。ドアや発電機へ使用する。";

    private bool HeldBy(Player player) => player.CurrentItem is { } item && Of(item.Serial) is Toolbox toolbox && ReferenceEquals(toolbox, this);
    private bool Ready => Time.time >= cooldownAt;

    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw || !HeldBy(ev.Player)) return;

        ev.IsAllowed = false;
        mode = mode == UtilityMode.Work ? UtilityMode.MaintenanceLock : UtilityMode.Work;
        ev.Player.SendHint(CurrentHint(), 3f);
    }

    private void InteractDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (!HeldBy(ev.Player) || !Ready) return;

        if (mode == UtilityMode.Work)
        {
            if (ev.Door is not BreakableDoor breakable || breakable.IsDestroyed) return;
            breakable.TryBreak();
        }
        else
        {
            if (ev.Door.IsLocked) return;
            ev.IsAllowed = false;
            ev.Door.IsOpened = false;
            ev.Door.Lock(DoorLockReason.Lockdown079, true);
        }

        cooldownAt = Time.time + 60f;
        ev.Player.SendHint("Toolbox: クールダウン 60 秒", 2f);
    }

    private void InteractGenerator(PlayerInteractingGeneratorEventArgs ev)
    {
        if (!HeldBy(ev.Player) || mode != UtilityMode.Work || !Ready) return;

        ev.IsAllowed = true;
        ev.Generator.IsUnlocked = true;
        cooldownAt = Time.time + 60f;
    }

    private void FlipCoin(PlayerFlippingCoinEventArgs ev)
    {
        if (!HeldBy(ev.Player) || mode != UtilityMode.Work || !Ready) return;

        Door nearest = null;
        float distance = 3f;
        foreach (Door door in Door.List)
        {
            float candidate = Vector3.Distance(door.Position, ev.Player.Position);
            if (candidate < distance)
            {
                nearest = door;
                distance = candidate;
            }
        }

        if (nearest is not BreakableDoor breakable || !breakable.IsDestroyed) return;

        ev.IsAllowed = false;
        breakable.TryRepair();
        cooldownAt = Time.time + 60f;
    }

    private string CurrentHint() => mode == UtilityMode.Work
        ? "Toolbox: 作業\nドアを破壊 / 近くでコインを投げて修理 / 発電機を解錠"
        : "Toolbox: メンテナンスロック\nドアを閉じて 30 秒ロック";

    protected override void OnEquipped(Player player) => player.SendHint(CurrentHint(), 3f);

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.InteractingDoor += OnInteractingDoor;
        PlayerEvents.InteractingGenerator += OnInteractingGenerator;
        PlayerEvents.FlippingCoin += OnFlippingCoin;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.InteractingDoor -= OnInteractingDoor;
            PlayerEvents.InteractingGenerator -= OnInteractingGenerator;
            PlayerEvents.FlippingCoin -= OnFlippingCoin;
            hooked = false;
        });
    }

    private static void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (Of(ev.Player.CurrentItem) is Toolbox toolbox)
            toolbox.InteractDoor(ev);
    }

    private static void OnInteractingGenerator(PlayerInteractingGeneratorEventArgs ev)
    {
        if (Of(ev.Player.CurrentItem) is Toolbox toolbox)
            toolbox.InteractGenerator(ev);
    }

    private static void OnFlippingCoin(PlayerFlippingCoinEventArgs ev) =>
        (Of(ev.CoinItem.Serial) as Toolbox)?.FlipCoin(ev);
}
