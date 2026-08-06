using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;
using Sliced.API.Features;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Omega-1 本隊。</summary>
public sealed class LwsWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("MtfUnit Omega 1 designated Low Left Hand HasEntered AllRemaining .", $"<color={ServerColors.Silver}><b>機動部隊Omega-1 \"Law's Left Hand\"</color>が施設に到着しました。残存する全職員は、機動部隊が目的地に到着するまで、標準避難プロトコルに従って行動してください。");

    public override string Name => "MTF Omega-1";

    public override Faction Faction => Faction.FoundationStaff;

    public override string Theme => "./WaveThemes/_w_lws.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<LwsJudgement>(isForced: true),
        SpawnSetRoleDefinition.Custom<LwsLiaison>(),
        SpawnSetRoleDefinition.Custom<LwsForensic>(),
        SpawnSetRoleDefinition.Custom<LwsAgent>(count: 99),
    ];
}
