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
/// DANTE 陣営です。<see cref="DanteBattle"/> の間だけ存在します。
///
/// <para>
/// 旧 <c>DanteVictoryDefinitionSource</c> (優先度 20・<c>requiresVanillaEndLock</c>) の置き換えです。
/// ボスが健在な間はこのチームに生存者が居るのでラウンドは終わらず、
/// 討伐隊が全滅すれば <see cref="VictoryCondition.LastStanding"/> で DANTE の勝ち、
/// ボスを倒せばチームが空になり討伐側 (カオス) の勝ちで終わります。
/// 中央触手も同じチームなので、SCP 陣営を汚しません。
/// </para>
/// </summary>
public sealed class DanteTeam : CustomTeam
{
    public override string Name => "DANTE";

    public override string CassieName => "Dan Tay";

    public override string Color => "#ff1a1a";

    public override VictoryCondition Victory => VictoryCondition.LastStanding(priority: 20);

    public override bool UsesVanillaEnding => false;

    public override string VictoryText => "<b><size=80><color=#ff1a1a>DANTE</color>の勝利</size></b>";

    protected override bool IncludesVanilla(Player player) => false;
}
