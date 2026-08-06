using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Structs;

namespace AntiMeme.Roles.Moderators;

public sealed class ModeratorRole : ModeratorRoleBase
{
    public override string Name => "Law's Left Hand: Moderator";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#FF96DE><b>Law's Left Hand: Moderator</b></color>";

    /// <inheritdoc/>
    public override string Objective => "正しくモデレーション処置を行い、秩序を回復し安全なシャープ鯖を死守せよ。";
    public override string Description => "Omega-1内に存在する極秘治安維持隊\n正しくモデレーション処置を行い、\n秩序を回復し、安全なシャープ鯖を死守せよ！";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override float? MaxHealth => 999f;
    public override string CustomInfo => "<color=#ff69b4>Law's Left Hand Moderator</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardMTFCaptain, ItemType.Radio ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(ArmorVip) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo9x19] = 80 };
}
