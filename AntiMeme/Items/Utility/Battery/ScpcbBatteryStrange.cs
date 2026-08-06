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

public sealed class ScpcbBatteryStrange : ScpcbBatteryBase, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.To<ScpcbBattery18V>(),
        Coarse = Scp914Rule.To<ScpcbBattery9V>(),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.Destroy,
        VeryFine = Scp914Rule.Destroy,
    };

    public override string Name => "Strange Battery";

    /// <inheritdoc/>
    protected override string PickupModel => "BatteryStrange";
    public override string Description => "異常な電池。触れるだけで危険な電流を放つ。";
    protected override BatteryBehavior Behavior => BatteryBehavior.Lethal;
}
