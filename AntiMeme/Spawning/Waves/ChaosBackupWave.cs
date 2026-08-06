using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.ChaosInsurgency;

namespace AntiMeme.Spawning.Waves;

/// <summary>カオス・インサージェンシー増援。</summary>
public sealed class ChaosBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ($"Attention All personnel . Detected {spawnCount} Chaos Insurgency Forces in Gate A . Please Terminate Them", $"全職員に通達。Gate Aに{spawnCount}人の<color=#228b22>カオス・インサージェンシー</color>部隊が検出されました。<split>見つけ次第終了してください。");

    public override string Name => "カオス・インサージェンシー (増援)";

    public override Faction Faction => Faction.FoundationEnemy;

    public override bool IsMiniWave => true;

    public override int DefaultWeight => 100;

    public override string Theme => "./WaveThemes/_w_chaos.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<ChaosSignal>(isForced: true),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.ChaosMarauder, count: 2),
        SpawnSetRoleDefinition.Custom<ChaosPenal>(),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.ChaosRifleman, count: 99),
    ];
}
