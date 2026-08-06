using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Nu-7 増援。半数だけ湧きます。</summary>
public sealed class HdBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("Her man down Backup unit has entered the facility .", "<color=#353535>下される鉄槌 予備部隊</color>が施設に到着しました。");

    public override string Name => "MTF Nu-7 (増援)";

    public override Faction Faction => Faction.FoundationStaff;

    public override bool IsMiniWave => true;

    public override int DefaultWeight => 20;

    public override float Ratio => 0.5f;

    public override string Theme => "./WaveThemes/_w_hd.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<HdCommander>(isForced: true),
        SpawnSetRoleDefinition.Custom<HdShotgunner>(count: 2),
        SpawnSetRoleDefinition.Custom<HdDisarmer>(count: 2),
        SpawnSetRoleDefinition.Custom<HdShielder>(count: 2),
        SpawnSetRoleDefinition.Custom<HdInfantry>(count: 99),
    ];
}
