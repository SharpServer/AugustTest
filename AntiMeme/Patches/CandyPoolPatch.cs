using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using InventorySystem.Items.Usables.Scp330;

namespace AntiMeme.Patches;

/// <summary>
/// SCP-330 のキャンディ一覧を、季節に関係なく「全 13 色」に固定します。
///
/// <para>
/// バニラは <see cref="Scp330Candies.NormalCandies"/> (8 色) と
/// <see cref="Scp330Candies.HauntedCandies"/> (13 色) をハロウィンかどうかで切り替えます。
/// 灰・白・茶・橙・Evil の 5 色は Haunted 側にしか無いため、
/// 通常期はそもそも引けません。
/// </para>
/// <para>
/// ここでは通常版がある色は通常版を使い、Haunted 側にしか無い色だけ Haunted 版を足します。
/// 色は年中揃い、効果は通常どおりになります。
/// </para>
/// </summary>
[HarmonyPatch(typeof(Scp330Candies), nameof(Scp330Candies.Candies), MethodType.Getter)]
public static class CandyPoolPatch
{
    private static ICandy[] pool;

    [HarmonyPrefix]
    private static bool CandiesPrefix(ref ICandy[] __result)
    {
        __result = pool ??= Build();

        return false;
    }

    private static ICandy[] Build()
    {
        HashSet<CandyKindID> normalKinds = new HashSet<CandyKindID>(Scp330Candies.NormalCandies.Select(candy => candy.Kind));

        return Scp330Candies.NormalCandies
            .Concat(Scp330Candies.HauntedCandies.Where(candy => !normalKinds.Contains(candy.Kind)))
            .ToArray();
    }
}
