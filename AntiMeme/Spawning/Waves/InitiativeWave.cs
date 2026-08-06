using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.Initiative;
using Sliced.API.Features;

namespace AntiMeme.Spawning.Waves;

/// <summary>境界線イニシアチブ。</summary>
public sealed class InitiativeWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ($"Attention All personnel . Detected {spawnCount} X Power Forces in Gate B . Please Terminate Them", $"全職員に通達。Gate Bに{spawnCount}人の<b><color={ServerColors.BlueGreen}>境界線イニシアチブ</color></b>部隊が検出されました。<split>見つけ次第終了してください。");

    public override string Name => "境界線イニシアチブ";

    public override Faction Faction => Faction.FoundationEnemy;

    public override string Theme => "./WaveThemes/_w_initiative.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<InitiativeWolf>(isForced: true),
    ];
}
