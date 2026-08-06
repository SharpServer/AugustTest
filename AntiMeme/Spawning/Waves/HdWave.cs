using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Nu-7 本隊。</summary>
public sealed class HdWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("MtfUnit Nu 7 Designated Hammer down HasEntered AllRemaining", "<color=#353535>機動部隊Nu-7 \"下される鉄槌\"</color>が施設に到着しました。");

    public override string Name => "MTF Nu-7";

    public override Faction Faction => Faction.FoundationStaff;

    public override int DefaultWeight => 20;

    public override string Theme => "./WaveThemes/_w_hd.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<HdMarshal>(),
        SpawnSetRoleDefinition.Custom<HdCommander>(count: 2, isForced: true),
        SpawnSetRoleDefinition.Custom<HdShotgunner>(count: 2),
        SpawnSetRoleDefinition.Custom<HdDisarmer>(count: 2),
        SpawnSetRoleDefinition.Custom<HdShielder>(count: 2),
        SpawnSetRoleDefinition.Custom<HdInfantry>(count: 99),
    ];
}
