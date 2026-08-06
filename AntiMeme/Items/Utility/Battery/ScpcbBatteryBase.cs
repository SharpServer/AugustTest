using AntiMeme.Items.Utility.Snav;
using AntiMeme.Items.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Scp914;
using InventorySystem.Items.Radio;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Features;
using UnityEngine;

using LabRadioItem = LabApi.Features.Wrappers.RadioItem;

namespace AntiMeme.Items.Utility.Battery;

/// <summary>SCPCB 電池の充電・無効・感電挙動をまとめる土台です。</summary>
public abstract class ScpcbBatteryBase : CustomUsable
{
    private const float HudInterval = 1f;
    private const float ResultDisplaySeconds = 2.5f;

    private static bool hooked;
    private readonly Dictionary<Player, int> selectedTargets = new();
    private CoroutineHandle hudLoop;
    private float keepInfoUntil;

    protected ScpcbBatteryBase() => Hook();

    public override ItemType BaseType => ItemType.Medkit;

    protected enum BatteryBehavior
    {
        Recharge,
        Inert,
        Lethal,
    }

    protected virtual BatteryBehavior Behavior => BatteryBehavior.Recharge;
    protected virtual float RechargeAmount => 100f;
    protected virtual bool FullRecharge => true;
    protected virtual string InertText => "この電池は電圧が合わず、NVG / S-Nav / Radio には使えません。";
    protected virtual string LethalText => "異常な電池が強烈な電流を放っています。";

    protected string ChargeLabel => FullRecharge ? "100%" : $"+{RechargeAmount:0}%";

    protected override bool CancelVanillaUse => true;

    protected override void OnCreated()
    {
        base.OnCreated();
        if (Behavior == BatteryBehavior.Lethal && Owner is { } owner)
            Shock(owner);
    }

    protected override void OnPickedUp(Player player)
    {
        base.OnPickedUp(player);
        selectedTargets.TryAdd(player, 0);
        if (Behavior == BatteryBehavior.Lethal)
            Shock(player);
    }

    protected override void OnEquipped(Player player)
    {
        base.OnEquipped(player);
        selectedTargets.TryAdd(player, 0);
        StopHudLoop();
        hudLoop = PlayerScope.Of(player).RunLoop(HudInterval, owner =>
        {
            if (owner.CurrentItem is not { } held || held.Serial != Serial)
            {
                StopHudLoop();
                return;
            }

            if (Time.time >= keepInfoUntil)
                owner.SendHint(BuildHud(owner), HudInterval + 0.25f);
        });
        player.SendHint(BuildHud(player), HudInterval + 0.25f);
    }

    protected override void OnUnequipped(Player player)
    {
        StopHudLoop();
        base.OnUnequipped(player);
    }

    protected override void OnReleased()
    {
        StopHudLoop();
        selectedTargets.Clear();
        base.OnReleased();
    }

    protected override bool CanUse(Player player)
    {
        if (Behavior == BatteryBehavior.Lethal)
            return true;
        return Behavior == BatteryBehavior.Recharge && GetSelectedTarget(player) is { CanRecharge: true };
    }

    protected override void OnUse(Player player)
    {
        if (Behavior == BatteryBehavior.Lethal)
        {
            Shock(player);
            return;
        }

        IRechargeableBatteryTarget target = GetSelectedTarget(player);
        if (target is null || !target.CanRecharge)
            return;

        float before = target.Percent;
        target.Recharge(RechargeAmount, FullRecharge);
        keepInfoUntil = Time.time + ResultDisplaySeconds;
        player.SendHint(
            $"<color=#88ff88>{target.DisplayName}</color> を充電しました: {before:0}% -> {target.Percent:0}%",
            ResultDisplaySeconds);
    }

    private void CycleTarget(Player player)
    {
        List<IRechargeableBatteryTarget> targets = FindTargets(player);
        if (targets.Count == 0)
        {
            selectedTargets[player] = 0;
            return;
        }

        int current = selectedTargets.GetValueOrDefault(player, 0);
        selectedTargets[player] = (current + 1) % targets.Count;
        keepInfoUntil = 0f;
        player.SendHint(BuildHud(player), HudInterval + 0.25f);
    }

    private IRechargeableBatteryTarget GetSelectedTarget(Player player)
    {
        List<IRechargeableBatteryTarget> targets = FindTargets(player);
        if (targets.Count == 0)
            return null;

        int selected = Mathf.Clamp(selectedTargets.GetValueOrDefault(player, 0), 0, targets.Count - 1);
        selectedTargets[player] = selected;
        return targets[selected];
    }

    private List<IRechargeableBatteryTarget> FindTargets(Player player)
    {
        return player.Items
            .Where(item => item is not null && item.Serial != Serial)
            .Select(item => item switch
            {
                Scp1344Item nvg when CustomItem.Of(item.Serial) is CustomNvg custom =>
                    (IRechargeableBatteryTarget)new NvgBatteryTarget(nvg, custom.Name),
                LabRadioItem radio when CustomItem.Of(item.Serial) is SNAV300 or SNAV310 or SNAVUltimate =>
                    new RadioBatteryTarget(radio, CustomItem.Of(item.Serial).Name, "S-Nav"),
                LabRadioItem radio when CustomItem.Of(item.Serial) is null =>
                    new RadioBatteryTarget(radio, "Radio", "Radio"),
                _ => null,
            })
            .Where(target => target is not null)
            .Cast<IRechargeableBatteryTarget>()
            .ToList();
    }

    private string BuildHud(Player player)
    {
        if (Behavior == BatteryBehavior.Inert)
            return $"<color=#ffd966>{Name}</color>\n<color=#aaaaaa>{InertText}</color>";
        if (Behavior == BatteryBehavior.Lethal)
            return $"<color=#ff5555>{Name}</color>\n<color=#ff7777>{LethalText}</color>";

        IRechargeableBatteryTarget target = GetSelectedTarget(player);
        if (target is null)
            return $"<color=#ffd966>{Name}</color>\n<color=#ff7777>充電可能な対象がありません</color>";

        string stateColor = target.CanRecharge ? "#88ff88" : "#aaaaaa";
        string action = target.CanRecharge
            ? $"充電量: <color=#88ff88>{ChargeLabel}</color>"
            : "<color=#aaaaaa>満充電</color>";
        return $"<color=#ffd966>{Name}</color>\n" +
               $"対象: <color=#88ccff>{target.DisplayName}</color> <color=#aaaaaa>({target.Kind})</color>\n" +
               $"電池: <color={stateColor}>{target.Percent:0}%</color>\n{action}\n" +
               "<color=#888888>ドロップ: 対象切替 / 使用: 充電</color>";
    }

    private void StopHudLoop()
    {
        if (hudLoop.IsRunning)
            Timing.KillCoroutines(hudLoop);
        hudLoop = default;
    }

    private void Shock(Player player)
    {
        if (player is not { IsDestroyed: false, IsAlive: true })
            return;

        player.SendHint($"<color=#ff5555>{Name}</color>\n<color=#ff7777>{LethalText}</color>", 2.5f);
        Timing.CallDelayed(0.1f, () =>
        {
            if (player is { IsDestroyed: false, IsAlive: true })
                player.Kill("Strange Battery");
        });
    }

    private static void Hook()
    {
        if (hooked)
            return;

        hooked = true;
        PlayerEvents.Left += OnLeft;
        ServerEvents.RoundRestarted += OnRoundRestarted;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.Left -= OnLeft;
            ServerEvents.RoundRestarted -= OnRoundRestarted;
            hooked = false;
        });
    }

    /// <inheritdoc/>
    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw || Behavior != BatteryBehavior.Recharge) return;

        ev.IsAllowed = false;
        CycleTarget(ev.Player);
    }

    private static void OnLeft(PlayerLeftEventArgs ev)
    {
        foreach (ScpcbBatteryBase battery in CustomItem.Tracked.OfType<ScpcbBatteryBase>())
            battery.selectedTargets.Remove(ev.Player);
    }

    private static void OnRoundRestarted()
    {
        foreach (ScpcbBatteryBase battery in CustomItem.Tracked.OfType<ScpcbBatteryBase>())
        {
            battery.StopHudLoop();
            battery.selectedTargets.Clear();
        }
    }
}
