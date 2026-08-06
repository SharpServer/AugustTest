using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;
using AntiMeme.Roles.Guards;
using AntiMeme.Teams.Factions;
using Sliced.API.Features;

namespace AntiMeme.Spawning.Waves;

/// <summary>ラウンド序盤の警備班。</summary>
public sealed class SecurityTeamWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("Attention All personnel Security Team has entered the facility", $"全職員に通達。<color={CustomTeam.Get<GuardsTeam>().Color}>保安部隊</color>が施設に到着しました。");

    public override string Name => "警備班";

    public override Faction Faction => Faction.FoundationStaff;

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<NtfCadet>(isForced: true),
        SpawnSetRoleDefinition.Custom<SecurityTeamGuard>(count: 99),
    ];
}

// ===== 敵対勢力 =====
