using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.Warriors;

namespace AntiMeme.Spawning.Waves;

/// <summary>クリスマスの雪の戦士 (増援)。</summary>
public sealed class SnowBackupWave : WaveSet
{
    public override string Name => "雪の戦士 (増援)";

    public override Faction Faction => Faction.FoundationEnemy;

    public override bool IsMiniWave => true;

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<SnowWarrior>(count: 99),
    ];
}
