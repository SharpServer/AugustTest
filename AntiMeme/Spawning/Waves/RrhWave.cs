using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;
using Sliced.API.Features;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Alpha-1 本隊。</summary>
public sealed class RrhWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("MtfUnit Alpha 1 designated Red Right Hand HasEntered AllRemaining", $"<color={ServerColors.Red}><b>機動部隊Alpha-1 \"Red Right Hand\"</color>が施設に到着しました。残存する全職員は、機動部隊が目的地に到着するまで、標準避難プロトコルに従って行動してください。");

    public override string Name => "MTF Alpha-1";

    public override Faction Faction => Faction.FoundationStaff;

    public override string Theme => "./WaveThemes/_w_rrh.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<RrhWarden>(isForced: true),
        SpawnSetRoleDefinition.Custom<RrhEnforcer>(),
        SpawnSetRoleDefinition.Custom<RrhAegis>(),
        SpawnSetRoleDefinition.Custom<RrhAssaulter>(),
    ];
}
