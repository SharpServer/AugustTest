using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.Fifthist;

namespace AntiMeme.Spawning.Waves;

/// <summary>第五教会本隊。既定では抽選に出ません。</summary>
public sealed class FifthistWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ($"Attention All personnel Detected {spawnCount} 5 5 5 Forces in Gate B", $"Gate Bに{spawnCount}人の<color=#ff0090>第五主義者</color>が検出されました。");

    public override string Name => "第五教会";

    public override Faction Faction => Faction.FoundationEnemy;

    public override string Theme => "./WaveThemes/_w_fifthists.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<FifthistPriest>(isForced: true),
        SpawnSetRoleDefinition.Custom<FifthistRescure>(count: 3),
        SpawnSetRoleDefinition.Custom<FifthistGuidance>(),
        SpawnSetRoleDefinition.Custom<FifthistMindblaster>(),
        SpawnSetRoleDefinition.Custom<FifthistConvert>(count: 99),
    ];
}
