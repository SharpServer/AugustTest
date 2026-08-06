using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using AntiMeme.Teams.Factions;
using InventorySystem.Items.Radio;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using Sliced.API.Features;
using UnityEngine;

using LabRadioItem = LabApi.Features.Wrappers.RadioItem;

namespace AntiMeme.Items.Utility.Snav;

public sealed class SNAV300 : SnavBase, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => SnavRules.Standard;

    private static readonly IReadOnlyCollection<RoomName> RoomTargets =
    [
        RoomName.Lcz914, RoomName.Hcz127, RoomName.HczAcroamaticAbatement,
        RoomName.Hcz079, RoomName.HczWarhead, RoomName.EzIntercom, RoomName.EzGateA, RoomName.EzGateB,
    ];

    public override string Name => "S-Nav 300";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;
    public override string Description => "近くのユニークな部屋について調べられる。\n投げて使用可能";
    protected override IReadOnlyCollection<RoomName> Targets => RoomTargets;
}
