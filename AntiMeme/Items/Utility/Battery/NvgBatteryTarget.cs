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

internal sealed class NvgBatteryTarget : IRechargeableBatteryTarget
{
    private readonly Scp1344Item item;
    public NvgBatteryTarget(Scp1344Item item, string displayName) { this.item = item; DisplayName = displayName; }
    public string Kind => "NVG";
    public string DisplayName { get; }
    public float Percent => CustomNvg.GetBattery(item.Serial, 100f);
    public bool CanRecharge => Percent < 100f;
    public void Recharge(float amount, bool fullRecharge) => CustomNvg.SetBattery(item.Serial, fullRecharge ? 100f : Percent + amount, true);
}
