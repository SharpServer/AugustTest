using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Roles.GoC;

public sealed class GoCMedic : GoCRole
{
    public override string Name => "GoC: Broken Dagger 医療スペシャリスト";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#0000c8>Broken Dagger: Medic</color>";
    public override string Description => "負傷した部隊の治療等を行う\nPassive: VERITAS\n遠くにいる敵等を認識できる";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSpecialist;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Global Occult Collision: Broken Dagger Medic";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunE11SR, ItemType.KeycardMTFOperative, ItemType.SCP500,
      ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(AdvancedMedkit), typeof(AdvancedMedkit), typeof(AdvancedMedkit), typeof(ArmorInfantry) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo556x45] = 140 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Scp1344>() ];
}
