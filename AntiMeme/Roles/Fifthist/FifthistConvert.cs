using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using AntiMeme.Maps;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Fifthist;

public sealed class FifthistConvert : FifthistRole
{
    public override string Name => "第五教会 改宗者";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff5ffa>Fifthist: Convert</color>";

    /// <inheritdoc/>
    public override string Objective => "あなたは第五教会の新入りだ。第五とは何かについて考え、理解し、そして従いなさい。";
    public override string Description => "貴方は新たに第五教会に加わった。全てを第五に捧げるのです。\nSCP-1425を使って、更に第五を広めろ！";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? SpawnPosition => SpawnPoints.ExitB;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "<color=#FF0090>Fifthist Convert</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCOM18, ItemType.Medkit, ItemType.ArmorLight ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(KeycardFifthist), typeof(Scp1425) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo762x39] = 170 };
}
