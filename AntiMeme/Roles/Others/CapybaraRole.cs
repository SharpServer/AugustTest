using System;
using System.Collections.Generic;
using AntiMeme.Abilities;
using AntiMeme.Items.Melee;
using AntiMeme.Maps;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MEC;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;

namespace AntiMeme.Roles.Others;

public sealed class CapybaraRole : OtherRole
{
    public override string Name => "Xx_CAPYBARA_xX";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ffffff>Xx_CAPYBARA_xX</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#ffffff>Unknown Forces</color>";

    /// <inheritdoc/>
    public override string Objective => "小さな姿で施設を駆け回り、Capybara Missileで混沌を撒き散らせ。";
    public override string Description => "WTF";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? Scale => Vector3.one * 0.35f;
    public override IReadOnlyList<ItemType> Items => [];
    public override IReadOnlyList<Type> CustomItems => [ typeof(CapybaraMissile) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 35 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Fade>(255) ];

    protected override void OnSpawned()
    {
        RoleWear wear = RoleWear.AttachCapybara(Player);
        if (wear != null) Scope.OnDispose(_ => wear.Dispose());
    }
}
