using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.ChaosInsurgency;

namespace AntiMeme.Spawning.Waves;

/// <summary>カオス・インサージェンシー本隊。</summary>
public sealed class ChaosInsurgencyWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ($"Attention All personnel . Detected {spawnCount} Chaos Insurgency Forces in Gate A . Please Terminate Them", $"全職員に通達。Gate Aに{spawnCount}人の<color=#228b22>カオス・インサージェンシー</color>部隊が検出されました。<split>見つけ次第終了してください。");

    public override string Name => "カオス・インサージェンシー";

    public override Faction Faction => Faction.FoundationEnemy;

    public override int DefaultWeight => 100;

    public override string Theme => "./WaveThemes/_w_chaos.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<ChaosCommando>(),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.ChaosRepressor, count: 2),
        SpawnSetRoleDefinition.Custom<ChaosSignal>(count: 2),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.ChaosMarauder, count: 2),
        SpawnSetRoleDefinition.Custom<ChaosTacticalUnit>(count: 2),
        SpawnSetRoleDefinition.Custom<ChaosPenal>(count: 2),
        SpawnSetRoleDefinition.Custom<ChaosSniper>(count: 2),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.ChaosRifleman, count: 99),
    ];
}
