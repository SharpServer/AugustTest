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

internal sealed class RadioBatteryTarget : IRechargeableBatteryTarget
{
    private readonly LabRadioItem item;
    public RadioBatteryTarget(LabRadioItem item, string displayName, string kind) { this.item = item; DisplayName = displayName; Kind = kind; }
    public string Kind { get; }
    public string DisplayName { get; }
    public float Percent => item.BatteryPercent;
    public bool CanRecharge => Percent < 100f;
    public void Recharge(float amount, bool fullRecharge) => item.BatteryPercent = (byte)Mathf.Clamp(fullRecharge ? 100f : item.BatteryPercent + Mathf.RoundToInt(amount), 0, 100);
}
