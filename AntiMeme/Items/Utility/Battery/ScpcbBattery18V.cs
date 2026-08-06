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

public sealed class ScpcbBattery18V : ScpcbBatteryBase, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.To<ScpcbBattery9V>(),
        Coarse = Scp914Rule.To<ScpcbBattery9V>(),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.To<ScpcbBatteryStrange>(),
        VeryFine = Scp914Rule.To<ScpcbBatteryStrange>(),
    };

    public override string Name => "18V Battery";

    /// <inheritdoc/>
    protected override string PickupModel => "Battery18V";
    public override string Description => "高出力の角形電池。NVG、S-Nav、Radioには電圧が合わず、充電には使えない。";
    protected override BatteryBehavior Behavior => BatteryBehavior.Inert;
}
