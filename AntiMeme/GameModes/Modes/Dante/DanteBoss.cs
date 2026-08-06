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
/// ボス本体の役職です。実体は <see cref="DanteBattle"/> が NPC として完全制御します。
///
/// <para>
/// HP は完全に仮想化されているので、ここで宣言する体力は「実ダメージを受けない」ための固定値です。
/// 素体が財団の隊長なのは、討伐側 (カオス) からバニラ上も確実に撃たれるためです。
/// </para>
/// </summary>
public sealed class DanteBoss : CustomRole
{
    /// <summary>実 HP は触らせません (幕進行と撃破は仮想 HP だけで決めます)。</summary>
    public const float PinnedHealth = 100000f;

    /// <summary>当たり判定 (NPC 本体の localScale)。見た目はスキマティック側で出します。</summary>
    public const float HitboxScale = 2.6f;

    /// <summary>NPC を立てるときの素体ロール。</summary>
    public const RoleTypeId BaseRoleType = RoleTypeId.NtfCaptain;

    public override string Name => "<color=#ff1a1a>DANTE ─ 業火の指揮者</color>";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff1a1a>DANTE ─ 業火の指揮者</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#ffffff>Unknown Forces</color>";

    /// <inheritdoc/>
    public override string Objective => "業火と触腕で討伐部隊を焼き払い、地上を支配せよ。";

    public override string Description => "地獄の業火を指揮する者。";

    public override CustomTeam Team => CustomTeam.Get<DanteTeam>();

    public override RoleTypeId BaseRole => BaseRoleType;

    public override float? MaxHealth => PinnedHealth;

    public override Vector3? Scale => Vector3.one * HitboxScale;

    public override string CustomInfo => "DANTE";
}
