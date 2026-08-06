using InventorySystem.Items.Usables;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// 継続回復です。SCP-500 と同じ仕組み (<see cref="RegenerationProcess"/>) に相乗りします。
///
/// <para>
/// 旧実装は EXILED の <c>NaturalHeal</c> 効果を使っていましたが、
/// 現行のゲーム本体にその効果はありません。継続回復は効果ではなく
/// <see cref="UsableItemsController"/> が毎フレーム回すプロセスとして表現されています。
/// </para>
/// </summary>
public static class Regeneration
{
    /// <summary>
    /// 毎秒 <paramref name="healthPerSecond"/> ずつ、<paramref name="duration"/> 秒かけて回復させます。
    /// </summary>
    public static void Apply(Player player, float healthPerSecond, float duration)
    {
        RegenerationProcess process = new RegenerationProcess(
            AnimationCurve.Constant(0f, duration, 1f),
            speedMultiplier: 1f,
            healthPointsMultiplier: healthPerSecond);

        UsableItemsController.GetHandler(player.ReferenceHub).ActiveRegenerations.Add(process);
    }
}
