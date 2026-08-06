using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.Warriors;

namespace AntiMeme.Spawning.Waves;

/// <summary>クリスマスの雪の戦士。</summary>
public sealed class SnowWave : WaveSet
{
    public override string Name => "雪の戦士";

    public override Faction Faction => Faction.FoundationEnemy;

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<SnowWarrior>(count: 99),
    ];
}
