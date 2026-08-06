using AdminToys;
using HarmonyLib;
using UnityEngine;

namespace AntiMeme.Patches;

/// <summary>
/// 不可視の相互作用トイのコライダーをトリガーにします。
///
/// <para>
/// バニラは実体のあるコライダーを付けるので、通り抜けられるはずの場所で
/// プレイヤーが引っかかります。当たり判定としては使うが押し返しはしない、が意図です。
/// </para>
/// </summary>
[HarmonyPatch(typeof(InvisibleInteractableToy), nameof(InvisibleInteractableToy.SetCollider))]
public static class InteractableToyColliderPatch
{
    [HarmonyPostfix]
    private static void Postfix(InvisibleInteractableToy __instance)
    {
        foreach (Collider collider in __instance.GetComponents<Collider>())
        {
            collider.isTrigger = true;
        }
    }
}
