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

public sealed class SNAVUltimate : SnavBase, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => SnavRules.Standard;

    private static readonly IReadOnlyCollection<RoomName> RoomTargets =
    [
        RoomName.Lcz914, RoomName.Lcz330, RoomName.LczGlassroom, RoomName.LczArmory,
        RoomName.LczCheckpointA, RoomName.LczCheckpointB, RoomName.Hcz127, RoomName.Hcz079,
        RoomName.HczWarhead, RoomName.HczTestroom, RoomName.Hcz049, RoomName.Hcz106,
        RoomName.HczTesla, RoomName.HczCheckpointA, RoomName.HczCheckpointB,
        RoomName.HczCheckpointToEntranceZone, RoomName.EzIntercom, RoomName.EzGateA, RoomName.EzGateB,
        RoomName.EzCollapsedTunnel, RoomName.EzOfficeSmall, RoomName.EzOfficeLarge,
    ];

    public override string Name => "S-Nav Ultimate";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.blue;
    public override string Description =>
        "SCP-914によって改良されたS-Nav。\n電池不要かつマップが大幅に拡張されており、SCPの情報も得られる。\n" +
        "より多くの、近くのユニークな部屋について調べられる。\n投げて使用可能";
    protected override IReadOnlyCollection<RoomName> Targets => RoomTargets;
    protected override bool UsesBattery => false;
    protected override bool DetectsScps => true;
}
