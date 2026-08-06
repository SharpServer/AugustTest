using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

using Random = UnityEngine.Random;

namespace AntiMeme.Abilities;

/// <summary>
/// 自分の周囲へ SCP-173 の分泌物を一度に撒きます。
/// </summary>
public sealed class PlaceTantrumAbility : AbilityBase
{
    private const int Count = 5;
    private const float Spread = 5f;

    /// <inheritdoc/>
    public override string Name => "汚物作戦";

    /// <inheritdoc/>
    public override string Description => "周囲に分泌物をまとめて撒く。";

    /// <inheritdoc/>
    public override float Cooldown => 80f;

    /// <summary>
    /// 指定した地点の周囲へ撒きます。マップ側の仕掛けからも同じ効果を出せるように公開しています。
    /// </summary>
    public static void Place(Vector3 center)
    {
        for (int i = 0; i < Count; i++)
        {
            Vector3 position = new Vector3(
                center.x + Random.Range(-Spread, Spread),
                center.y,
                center.z + Random.Range(-Spread, Spread));

            TantrumHazard.Spawn(position, Quaternion.identity, Vector3.one);
        }
    }

    /// <inheritdoc/>
    protected override void OnUsed() => Place(Player.Position);
}
