using System.Collections.Generic;
using System.Linq;
using AntiMeme.Audio;
using AntiMeme.Hud;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using AntiMeme.Maps;
using AntiMeme.Net;
using AntiMeme.Teams;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;
using Utils;

using ExiledNpc = Exiled.API.Features.Npc;
using ExiledPlayer = Exiled.API.Features.Player;
using Logger = LabApi.Features.Console.Logger;
using Random = UnityEngine.Random;

namespace AntiMeme.GameModes.Modes.Dante;

/// <summary>
/// ダンテ討伐部隊です。DANTE と互角に渡り合うための専用レイド役職で、
/// チームはカオス・インサージェンシー固定です。
///
/// <para>
/// 対ボス主力は <see cref="GunM82"/> (通常ダメージなので仮想 HP に素直に通ります)。
/// レールガン系は被弾フックを迂回し得るので採用しません。
/// </para>
/// </summary>
public sealed class DanteSlayer : CustomRole
{
    /// <summary>増援で立たせる位置。指定しなければバニラのスポーン地点です。</summary>
    public Vector3? Spot;

    public override string Name => "<color=#39ff14>ダンテ討伐部隊</color>";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#39ff14>Dante Slayer Unit</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#228b22>Chaos Insurgency</color>";

    /// <inheritdoc/>
    public override string Objective => "対物ライフルと制圧火器でDANTEを削り切り、業火の指揮者を討伐せよ。";

    public override string Description =>
        "緑の巨塊 DANTE を討つために編成された対異常存在制圧部隊。対物ライフルと制圧火器で粘体を削り切れ。";

    public override CustomTeam Team => CustomTeam.Get<ChaosInsurgencyTeam>();

    public override RoleTypeId BaseRole => RoleTypeId.ChaosRifleman;

    public override float? MaxHealth => 6500f;

    public override Vector3? SpawnPosition => Spot;

    public override IReadOnlyList<ItemType> Items =>
    [
        ItemType.Medkit,
        ItemType.SCP500,
        ItemType.SCP500,
        ItemType.Adrenaline,
        ItemType.GrenadeHE,
        ItemType.GrenadeHE,
    ];

    public override IReadOnlyList<System.Type> CustomItems =>
    [
        typeof(GunM82),
        typeof(GunSuperLogicer),
        typeof(ArmorVip),
        typeof(AdvancedMedkit),
    ];

    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    {
        [ItemType.Ammo556x45] = 1500,
        [ItemType.Ammo762x39] = 1200,
    };

    public override string CustomInfo => "DANTE SLAYER";
}
