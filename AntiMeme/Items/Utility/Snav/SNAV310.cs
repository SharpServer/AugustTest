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

public sealed class SNAV310 : SnavBase, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => SnavRules.Standard;

    private static readonly IReadOnlyCollection<RoomName> RoomTargets =
    [
        RoomName.Lcz914, RoomName.Lcz330, RoomName.LczGlassroom, RoomName.Hcz127,
        RoomName.HczAcroamaticAbatement, RoomName.Hcz079, RoomName.HczWarhead, RoomName.HczTestroom,
        RoomName.EzIntercom, RoomName.EzGateA, RoomName.EzGateB,
    ];

    public override string Name => "S-Nav 310 Navigator";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;
    public override string Description =>
        "S-Nav 300が改良され、電池不要かつマップが拡張されている。\n様々な近くのユニークな部屋について調べられる。\n投げて使用可能";
    protected override IReadOnlyCollection<RoomName> Targets => RoomTargets;
    protected override bool UsesBattery => false;
}
