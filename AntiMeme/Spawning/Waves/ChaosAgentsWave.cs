using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.ChaosInsurgency;

namespace AntiMeme.Spawning.Waves;

/// <summary>ラウンド序盤のカオス潜入者。</summary>
public sealed class ChaosAgentsWave : WaveSet
{
    public override string Name => "カオス潜入者";

    public override Faction Faction => Faction.FoundationEnemy;

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<ChaosIntruder>(count: 99),
    ];
}
