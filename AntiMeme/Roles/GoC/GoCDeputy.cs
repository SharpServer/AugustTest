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

public sealed class GoCDeputy : GoCRole
{
    public override string Name => "GoC: Broken Dagger 副官";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#0000c8>Broken Dagger: Deputy</color>";
    public override string Description => "部隊の任務遂行を補助する\nPassive: VERITAS\n遠くにいる敵等を認識できる";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSergeant;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Global Occult Collision: Broken Dagger Deputy";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCom45, ItemType.GunE11SR, ItemType.KeycardMTFOperative,
      ItemType.Medkit, ItemType.GrenadeHE, ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(FlashBangE), typeof(ArmorInfantry) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo556x45] = 140 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Scp1344>() ];
}
