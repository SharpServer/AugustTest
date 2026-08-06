using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;

namespace AntiMeme.Spawning.Waves;

/// <summary>世界オカルト連合増援。</summary>
public sealed class GoCBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ($"Attention All personnel Detected {spawnCount} G O C Forces in Gate B", $"Gate Bに{spawnCount}人の<color=#0000c8>世界オカルト連合</color>部隊が検出されました。");

    public override string Name => "世界オカルト連合 (増援)";

    public override Faction Faction => Faction.FoundationEnemy;

    public override bool IsMiniWave => true;

    public override string Theme => "./WaveThemes/_w_ungoc.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles => GoCWave.GoCRoles;
}
