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

namespace AntiMeme.Roles.GoC;

public sealed class GoCSquadLeader : GoCRole
{
    public override string Name => "GoC: Broken Dagger 班長";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#0000c8>Broken Dagger: Squad Leader</color>";
    public override string Description => "部隊を指揮し、任務を遂行させる。\nPassive: VERITAS\n遠くにいる敵等を認識できる";
    public override RoleTypeId BaseRole => RoleTypeId.NtfCaptain;
    public override float? MaxHealth => 110f;
    public override string CustomInfo => "Global Occult Collision: Broken Dagger Squad Leader";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunFRMG0, ItemType.KeycardMTFCaptain, ItemType.Medkit,
      ItemType.GrenadeHE, ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(GunGoCRailgun), typeof(SerumC), typeof(ArmorVip) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo556x45] = 140 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Scp1344>() ];
}
