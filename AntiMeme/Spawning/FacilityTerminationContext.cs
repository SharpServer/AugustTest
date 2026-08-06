using System;
using System.Collections.Generic;
using System.Linq;
using PlayerRoles;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;
using AntiMeme.Spawning.Waves;
using AntiMeme.GameModes.Modes;

namespace AntiMeme.Spawning;

/// <summary>
/// Facility Termination の文脈です。財団側は最終作戦だけ、敵対側は GoC が主力になります。
/// </summary>
/// <remarks>
/// 旧 <c>FacilityTerminationContexts</c> は「この 3 種だけを持つ辞書」だったので、
/// 表に無い波の重みは 0 でした。ここも同じく<b>ホワイトリスト</b>にします。
/// <c>base.WeightOf</c> にフォールバックすると NTF / RRH / SNE / 第五教会などが
/// 既定の重みのまま抽選に残り、最終作戦のはずのラウンドに通常部隊が湧きます。
/// </remarks>
public sealed class FacilityTerminationContext : SpawnContext
{
    public override string Name => "FacilityTermination";

    public override int WeightOf(WaveSet wave) => wave switch
    {
        LastOperationWave or LastOperationBackupWave => 100,
        GoCWave or GoCBackupWave => 70,
        ChaosInsurgencyWave or ChaosBackupWave => 30,
        _ => 0,
    };
}
