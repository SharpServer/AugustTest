using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Epsilon-11 本隊。</summary>
public sealed class NtfWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("MtfUnit Epsilon 11 Designated Ninetailedfox HasEntered AllRemaining", "<color=#5bc5ff>機動部隊Epsilon-11 \"九尾狐\"</color>が施設に到着しました。残存する全職員は、機動部隊が目的地に到着するまで、標準避難プロトコルに従って行動してください。");

    public override string Name => "MTF Epsilon-11";

    public override Faction Faction => Faction.FoundationStaff;

    public override int DefaultWeight => 80;

    public override string Theme => "./WaveThemes/_w_ntf.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<NtfGeneral>(),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.NtfCaptain, isForced: true),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.NtfSergeant, count: 2),
        SpawnSetRoleDefinition.Custom<NtfLieutenant>(count: 2),
        SpawnSetRoleDefinition.Custom<NtfDetainer>(),
        SpawnSetRoleDefinition.Custom<NtfFieldMedic>(),
        SpawnSetRoleDefinition.Custom<NtfGunslinger>(),
        SpawnSetRoleDefinition.Vanilla(RoleTypeId.NtfPrivate, count: 99, isForced: true),
    ];
}
