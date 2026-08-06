using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.GoC;

namespace AntiMeme.Spawning.Waves;

/// <summary>世界オカルト連合本隊。</summary>
public sealed class GoCWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ($"Attention All personnel Detected {spawnCount} G O C Forces in Gate B", $"Gate Bに{spawnCount}人の<color=#0000c8>世界オカルト連合</color>部隊が検出されました。");

    public override string Name => "世界オカルト連合";

    public override Faction Faction => Faction.FoundationEnemy;

    public override string Theme => "./WaveThemes/_w_ungoc.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles => GoCRoles;

    /// <summary>本隊と増援で同じ構成を使います。</summary>
    internal static IReadOnlyList<SpawnSetRoleDefinition> GoCRoles =>
    [
        SpawnSetRoleDefinition.Custom<GoCSquadLeader>(isForced: true),
        SpawnSetRoleDefinition.Custom<GoCDeputy>(isForced: true),
        SpawnSetRoleDefinition.Custom<GoCMedic>(),
        SpawnSetRoleDefinition.Custom<GoCThaumaturgist>(),
        SpawnSetRoleDefinition.Custom<GoCCommunications>(),
        SpawnSetRoleDefinition.Custom<GoCHoundDog>(),
        SpawnSetRoleDefinition.Custom<GoCOperative>(count: 99),
    ];
}
