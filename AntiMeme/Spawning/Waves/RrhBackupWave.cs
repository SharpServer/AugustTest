using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;
using Sliced.API.Features;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Alpha-1 増援。</summary>
public sealed class RrhBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("Red Right Hand Backup unit has entered the facility", $"<color={ServerColors.Red}><b>Red Right Hand 予備部隊</color>が施設に到着しました。");

    public override string Name => "MTF Alpha-1 (増援)";

    public override Faction Faction => Faction.FoundationStaff;

    public override bool IsMiniWave => true;

    public override string Theme => "./WaveThemes/_w_rrh.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<RrhEnforcer>(isForced: true),
        SpawnSetRoleDefinition.Custom<RrhAegis>(),
        SpawnSetRoleDefinition.Custom<RrhAssaulter>(),
    ];
}
