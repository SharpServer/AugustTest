using System.Collections.Generic;
using AntiMeme.Roles.Scps;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;
using Sliced.API.Features;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Omega-7。SCP-076 を連れてきます。</summary>
public sealed class PdxWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("MtfUnit Omega 7 designated hand r _SUFFIX_PLURAL_REGULAR o _SUFFIX_PLURAL_SYLLABIC HasEntered AllRemaining .", $"<color={ServerColors.Carmine}><b>機動部隊Omega-7 \"Pandra's Box\"</color>が施設に到着しました。");

    public override string Name => "MTF Omega-7";

    public override Faction Faction => Faction.FoundationStaff;

    public override string Theme => "./WaveThemes/_w_pdx.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<Scp076>(isForced: true),
        SpawnSetRoleDefinition.Custom<PdxWarden>(isForced: true),
        SpawnSetRoleDefinition.Custom<PdxWatcher>(count: 6),
    ];
}
