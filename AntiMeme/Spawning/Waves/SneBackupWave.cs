using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Eta-10 増援。</summary>
public sealed class SneBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("See no E be l Backup unit has entered the facility .", "<color=#FF1493>シー・ノー・イーヴル 予備部隊</color>が施設に到着しました。");

    public override string Name => "MTF Eta-10 (増援)";

    public override Faction Faction => Faction.FoundationStaff;

    public override bool IsMiniWave => true;

    public override string Theme => "./WaveThemes/_w_sne.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<SneGears>(),
        SpawnSetRoleDefinition.Custom<SneNeutralitist>(),
        SpawnSetRoleDefinition.Custom<SnePurify>(),
    ];
}
