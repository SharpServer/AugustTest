using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AntiMeme.Effects;
using CustomPlayerEffects;
using HarmonyLib;
using PlayerRoles.PlayableScps.Scp106;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Patches;

/// <summary>
/// SCP-106 の攻撃は「対象が <see cref="Traumatized"/> を持っているか」で
/// ポケット次元送りにするかを決めます。
/// <see cref="VisualTraumatized"/> が見た目のために点けたぶんで即死させないよう、
/// その判定だけを差し替えます。
///
/// <para>
/// 本物の SCP-106 が付けた <see cref="Traumatized"/> はそのまま効きます。
/// </para>
/// </summary>
[HarmonyPatch(typeof(Scp106Attack), nameof(Scp106Attack.ServerShoot))]
public static class Scp106VisualTraumatizedPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ServerShootTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        MethodInfo isEnabledGetter = AccessTools.PropertyGetter(typeof(StatusEffectBase), nameof(StatusEffectBase.IsEnabled));
        MethodInfo replacement = AccessTools.Method(typeof(Scp106VisualTraumatizedPatch), nameof(ShouldTriggerTraumatizedKill));
        bool replaced = false;

        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction instruction = codes[i];

            // GetEffect<Traumatized>() の直後の IsEnabled 呼び出しだけを差し替える。
            if (!replaced &&
                instruction.Calls(isEnabledGetter) &&
                i > 0 &&
                IsGetEffectCall(codes[i - 1], typeof(Traumatized)))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced = true;
            }

            yield return instruction;
        }

        if (!replaced)
        {
            // ゲーム本体の実装が変わるとここに落ちる。黙って壊れるより気付ける方がよい。
            Logger.Error(
                $"[AntiMeme] {nameof(Scp106Attack)}.{nameof(Scp106Attack.ServerShoot)} 内に " +
                $"Traumatized の IsEnabled 呼び出しが見つからず、パッチを適用できませんでした。");
        }
    }

    /// <summary>
    /// 差し替え後の判定です。見た目だけの分なら即死させません。
    /// </summary>
    public static bool ShouldTriggerTraumatizedKill(Traumatized traumatized) =>
        traumatized != null &&
        traumatized.IsEnabled &&
        !VisualTraumatized.ShouldSuppressScp106Kill(traumatized.Hub);

    private static bool IsGetEffectCall(CodeInstruction instruction, Type effectType)
    {
        if (instruction.operand is not MethodInfo method || !method.IsGenericMethod)
            return false;

        if (method.Name != nameof(PlayerEffectsController.GetEffect) ||
            method.DeclaringType != typeof(PlayerEffectsController))
        {
            return false;
        }

        Type[] arguments = method.GetGenericArguments();

        return arguments.Length == 1 && arguments[0] == effectType;
    }
}

/// <summary>
/// <see cref="VisualTraumatized"/> が握っているあいだ、
/// ゲーム側の <see cref="Traumatized"/> のライフサイクル処理を止めます。
/// </summary>
[HarmonyPatch(typeof(Traumatized))]
public static class VisualTraumatizedLifecyclePatch
{
    [HarmonyPatch(nameof(Traumatized.AllowEnabling), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool AllowEnablingPrefix(Traumatized __instance, ref bool __result)
    {
        if (!VisualTraumatized.ShouldSuppressScp106Kill(__instance.Hub))
            return true;

        __result = true;

        return false;
    }

    [HarmonyPatch(nameof(Traumatized.Enabled))]
    [HarmonyPrefix]
    private static bool EnabledPrefix(Traumatized __instance) =>
        !VisualTraumatized.ShouldSuppressScp106Kill(__instance.Hub);

    [HarmonyPatch(nameof(Traumatized.OnServerRoleChanged))]
    [HarmonyPrefix]
    private static bool OnServerRoleChangedPrefix(Traumatized __instance) =>
        !VisualTraumatized.ShouldSuppressScp106Kill(__instance.Hub);
}
