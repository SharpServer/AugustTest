using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>Facility Termination 用の最終作戦。</summary>
public sealed class LastOperationWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("MtfUnit Alpha 0 designated Last Operation HasEntered AllRemaining", "<color=red>機動部隊Alpha-0 \"最終指令\"</color>が施設に到着しました。");

    public override string Name => "最終作戦";

    public override Faction Faction => Faction.FoundationStaff;

    public override string Theme => "./WaveThemes/_w_lo.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<Sculpture>(count: 99, isForced: true),
    ];
}
