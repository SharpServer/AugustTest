using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Epsilon-11 増援。</summary>
public sealed class NtfBackupWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("Ninetailedfox Backup unit has entered the facility", "<color=#5bc5ff>九尾狐 予備部隊</color>が施設に到着しました。");

    public override string Name => "MTF Epsilon-11 (増援)";

    public override Faction Faction => Faction.FoundationStaff;

    public override bool IsMiniWave => true;

    public override int DefaultWeight => 80;

    public override string Theme => "./WaveThemes/_w_ntf.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.NtfSergeant, isForced: true),
        SpawnSetRoleDefinition.Custom<NtfDetainer>(),
        SpawnSetRoleDefinition.Custom<NtfFieldMedic>(),
        SpawnSetRoleDefinition.Custom<NtfGunslinger>(),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.NtfPrivate, count: 99, isForced: true),
    ];
}
