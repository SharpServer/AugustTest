using AntiMeme.Items.Utility.Snav;
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

public sealed class GoCCommunications : GoCRole
{
    public override string Name => "GoC: Broken Dagger 通信スペシャリスト";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#0000c8>Broken Dagger: Communications</color>";
    public override string Description => "SNAVを用いて探索を行う\nPassive: VERITAS\n遠くにいる敵等を認識できる";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSpecialist;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Global Occult Collision: Broken Dagger Communications";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunE11SR, ItemType.ParticleDisruptor, ItemType.KeycardMTFOperative,
      ItemType.Medkit, ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(SNAVUltimate), typeof(SerumC), typeof(ArmorInfantry) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo556x45] = 140 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Scp1344>() ];
}
