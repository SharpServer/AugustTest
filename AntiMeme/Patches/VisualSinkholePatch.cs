using AntiMeme.Effects;
using CustomPlayerEffects;
using HarmonyLib;
using PlayerRoles.FirstPersonControl.Thirdperson;

namespace AntiMeme.Patches;

/// <summary>
/// <see cref="VisualSinkhole"/> が握っている <see cref="Sinkhole"/> から、
/// 移動・スタミナ・足音のデバフだけを取り除きます。見た目は残します。
///
/// <para>
/// 本物の SCP-106 が付けた <see cref="Sinkhole"/> には触りません。
/// 判定は <see cref="VisualSinkhole.TryGetOwner"/> が持っていて、
/// 「この効果が自分で点けたぶんか」を見ています。
/// </para>
/// </summary>
[HarmonyPatch(typeof(Sinkhole))]
public static class VisualSinkholePatch
{
    [HarmonyPatch(nameof(Sinkhole.AllowEnabling), MethodType.Getter)]
    [HarmonyPrefix]
    private static bool AllowEnablingPrefix(Sinkhole __instance, ref bool __result)
    {
        if (!VisualSinkhole.TryGetOwner(__instance.Hub, out _))
            return true;

        __result = true;

        return false;
    }

    [HarmonyPatch(nameof(Sinkhole.MovementModifierActive), MethodType.Getter)]
    [HarmonyPostfix]
    private static void MovementModifierActivePostfix(Sinkhole __instance, ref bool __result)
    {
        if (VisualSinkhole.TryGetOwner(__instance.Hub, out _))
            __result = false;
    }

    [HarmonyPatch(nameof(Sinkhole.StaminaModifierActive), MethodType.Getter)]
    [HarmonyPostfix]
    private static void StaminaModifierActivePostfix(Sinkhole __instance, ref bool __result)
    {
        if (VisualSinkhole.TryGetOwner(__instance.Hub, out _))
            __result = false;
    }

    [HarmonyPatch(nameof(Sinkhole.SprintingDisabled), MethodType.Getter)]
    [HarmonyPostfix]
    private static void SprintingDisabledPostfix(Sinkhole __instance, ref bool __result)
    {
        if (VisualSinkhole.TryGetOwner(__instance.Hub, out _))
            __result = false;
    }

    [HarmonyPatch(nameof(Sinkhole.ProcessFootstepOverrides), typeof(AnimatedCharacterModel), typeof(float))]
    [HarmonyPrefix]
    private static bool ProcessFootstepOverridesPrefix(Sinkhole __instance, float dis, ref float __result)
    {
        if (!VisualSinkhole.TryGetOwner(__instance.Hub, out VisualSinkhole effect) ||
            effect.FootstepOverridesEnabled)
        {
            return true;
        }

        __result = dis;

        return false;
    }
}
