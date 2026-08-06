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
/// 中央触手 (Bacte 方式の弱点) です。これを全部壊すまでコアは無敵になります。
/// </summary>
public sealed class DanteTentacle : CustomRole
{
    /// <summary>弱点 1 つあたりの HP。</summary>
    public const float WeakPointHealth = 700f;

    /// <summary>NPC を立てるときの素体ロール。</summary>
    public const RoleTypeId BaseRoleType = RoleTypeId.Scp0492;

    /// <summary>立たせる位置。<see cref="DanteBattle"/> が生成時に渡します。</summary>
    public Vector3? Spot;

    public override string Name => "<color=#39ff14>DANTE の触腕</color>";

    public override string Description => "コアへ繋がる触手。断てばコアが露出する。";

    public override CustomTeam Team => CustomTeam.Get<DanteTeam>();

    public override RoleTypeId BaseRole => BaseRoleType;

    public override float? MaxHealth => WeakPointHealth;

    public override Vector3? Scale => Vector3.one * 1.3f;

    public override Vector3? SpawnPosition => Spot;

    public override string CustomInfo => "TENTACLE CORE";
}
