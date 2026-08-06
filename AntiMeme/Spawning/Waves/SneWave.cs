using System.Collections.Generic;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning.Waves;

/// <summary>MTF Eta-10 本隊。既定では抽選に出ません。</summary>
public sealed class SneWave : WaveSet
{
    public override (string Cassie, string Subtitle) Announcement(int spawnCount) =>
        ("MtfUnit Eta 10 designated See no E be l HasEntered AllRemaining . This forces work for the anti- me mu termination", 
            $"<color=#FF1493>機動部隊Eta-10 \"シー・ノー・イーヴル\"</color>が施設に到着しました。残存する全職員は、機動部隊が目的地に到着するまで、標準避難プロトコルに従って行動してください。<split>この部隊は反ミーム存在の終了の為に招集されました。");

    public override string Name => "MTF Eta-10";

    public override Faction Faction => Faction.FoundationStaff;

    public override string Theme => "./WaveThemes/_w_sne.ogg";

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<SneOperator>(isForced: true),
        SpawnSetRoleDefinition.Custom<SneGears>(count: 2),
        SpawnSetRoleDefinition.Custom<SneNeutralitist>(count: 2),
        SpawnSetRoleDefinition.Custom<SnePurify>(count: 2),
    ];
}
