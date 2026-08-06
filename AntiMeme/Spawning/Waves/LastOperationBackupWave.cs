using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>Facility Termination 用の最終作戦 (増援)。</summary>
public sealed class LastOperationBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("Last Operation Backup unit has entered the facility", "<color=red>最終指令 予備部隊</color>が施設に到着しました。");

    public override string Name => "最終作戦 (増援)";

    public override Faction Faction => Faction.FoundationStaff;

    public override bool IsMiniWave => true;

    public override string Theme => "./WaveThemes/_w_lo.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<Sculpture>(count: 99, isForced: true),
    ];
}
