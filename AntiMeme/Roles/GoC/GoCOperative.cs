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

public sealed class GoCOperative : GoCRole
{
    public override string Name => "GoC: Broken Dagger 工作員";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#0000c8>Broken Dagger: Operative</color>";
    public override string Description => "部隊の任務を遂行する\nPassive: VERITAS\n遠くにいる敵等を認識できる";
    public override RoleTypeId BaseRole => RoleTypeId.NtfPrivate;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Global Occult Collision: Broken Dagger Operative";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCrossvec, ItemType.GunShotgun, ItemType.KeycardMTFOperative,
      ItemType.Medkit, ItemType.Radio, ItemType.ArmorCombat ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(GoCRecruitPaper) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 140 };
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Scp1344>() ];
}
