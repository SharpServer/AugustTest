using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;
using Sliced.API.Features;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Omega-1 増援。</summary>
public sealed class LwsBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("Low Left Hand Backup unit has entered the facility .", $"<color={ServerColors.Silver}><b>Law's Left Hand 予備部隊</color>が施設に到着しました。");

    public override string Name => "MTF Omega-1 (増援)";

    public override Faction Faction => Faction.FoundationStaff;

    public override bool IsMiniWave => true;

    public override string Theme => "./WaveThemes/_w_lws.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<LwsLiaison>(isForced: true),
        SpawnSetRoleDefinition.Custom<LwsForensic>(),
        SpawnSetRoleDefinition.Custom<LwsAgent>(count: 99),
    ];
}
