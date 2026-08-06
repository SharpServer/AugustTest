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

public sealed class ScpcbBattery9V : ScpcbBatteryBase, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.To<ScpcbBattery18V>(),
        VeryFine = Scp914Rule.To<ScpcbBatteryStrange>(),
    };

    public override string Name => "9V Battery";

    /// <inheritdoc/>
    protected override string PickupModel => "Battery9V";
    public override string Description => "小型の角形電池。ドロップで充電対象を選び、使用すると対象のバッテリーを満充電にする。";
}
