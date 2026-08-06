using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.Fifthist;

namespace AntiMeme.Spawning.Waves;

/// <summary>第五教会増援。半数だけ湧きます。</summary>
public sealed class FifthistBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ($"Attention All personnel Detected {spawnCount} 5 5 5 Forces in Gate B", $"Gate Bに{spawnCount}人の<color=#ff0090>第五主義者</color>が検出されました。");

    public override string Name => "第五教会 (増援)";

    public override Faction Faction => Faction.FoundationEnemy;

    public override bool IsMiniWave => true;

    public override float Ratio => 0.5f;

    public override string Theme => "./WaveThemes/_w_fifthists.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<FifthistRescure>(),
        SpawnSetRoleDefinition.Custom<FifthistGuidance>(),
        SpawnSetRoleDefinition.Custom<FifthistConvert>(count: 99),
    ];
}
