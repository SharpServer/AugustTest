using System.Collections.Generic;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// 歩いた跡へ分泌物を垂れ流します。走って逃げながら道を塞ぐ用途です。
/// </summary>
public sealed class DropBiggerShitAbility : AbilityBase
{
    private const int Count = 8;
    private const float Interval = 0.55f;

    /// <inheritdoc/>
    public override string Name => "爺街道";

    /// <inheritdoc/>
    public override string Description => "しばらくのあいだ、足元へ分泌物を落とし続ける。";

    /// <inheritdoc/>
    public override float Cooldown => 120f;

    /// <inheritdoc/>
    protected override void OnUsed() => Scope.Track(Timing.RunCoroutine(Drop()));

    private IEnumerator<float> Drop()
    {
        for (int i = 0; i < Count; i++)
        {
            if (!Player.IsAlive) yield break;

            TantrumHazard.Spawn(Player.Position, Quaternion.identity, Vector3.one);

            yield return Timing.WaitForSeconds(Interval);
        }
    }
}
