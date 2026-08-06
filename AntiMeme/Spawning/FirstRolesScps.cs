using System.Collections.Generic;
using System.Linq;
using AntiMeme.Roles.Scps;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;

namespace AntiMeme.Spawning;

/// <summary>
/// ラウンド開始時の SCP 割り当てです。
///
/// <para>
/// 人数に応じて枠数が変わります。各行の <c>weight</c> が出やすさで、
/// <c>count</c> がそのまま上限になります。旧実装は重み表 (<c>WeightedRoleEntry</c>) と
/// 上限表 (<c>RoleLimitManager</c>) を別々に持ち、役職を <c>object</c> で入れていましたが、
/// どちらも 1 行の宣言に畳めます。
/// </para>
/// </summary>
public sealed class FirstRolesScps : SpawnSet
{
    public override string Name => "First Roles (SCPs)";

    public override string Description => "ラウンド開始時の SCP 割り当て。";

    public override int AllowedPlayerCount => Player.ReadyList.Count() switch
    {
        < 4 => 1,
        < 8 => 2,
        < 12 => 3,
        _ => 4,
    };

    public override IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles =>
    [
        SpawnSetRoleDefinition.Custom<Scp173>(),
        SpawnSetRoleDefinition.Custom<Scp106>(),
        SpawnSetRoleDefinition.Custom<Scp049>(),
        SpawnSetRoleDefinition.Custom<Scp079>(),
        SpawnSetRoleDefinition.Custom<Scp3114>(),

        // 特殊な SCP は出現頻度を抑える。
        SpawnSetRoleDefinition.Custom<Scp682>(weight: 0.5f),
        SpawnSetRoleDefinition.Custom<Scp966>(weight: 0.5f),
        SpawnSetRoleDefinition.Custom<Scp035>(weight: 0.4f),
        SpawnSetRoleDefinition.Custom<Scp610>(weight: 0.3f),
        SpawnSetRoleDefinition.Custom<Scp3005>(weight: 0.3f),
    ];
}

/// <summary>
