using HarmonyLib;
using PlayerRoles.Spectating;

namespace AntiMeme.Patches;

/// <summary>
/// 所有者を失ったロールの <see cref="SpectatableModuleBase.TargetHub"/> で
/// 例外を投げるのをやめ、null を返します。
///
/// <para>
/// バニラの getter は所有者が取れないと <c>InvalidOperationException</c> を投げます。
/// これを呼ぶ <c>SpectatorRole.Update()</c> → <c>NextTarget()</c> は例外を捕まえないため、
/// <c>_anySpectatorSelected</c> が立たないまま毎フレーム同じ例外を吐き続け、
/// 観戦者が対象を切り替えられなくなります
/// (一斉大量死と NPC の破棄が重なると再現します)。
/// </para>
/// <para>
/// 呼び出し側はどこも戻り値の null を確認してから使っているので、
/// null を返すだけで同じ意味を保てます。
/// </para>
/// </summary>
[HarmonyPatch(typeof(SpectatableModuleBase), nameof(SpectatableModuleBase.TargetHub), MethodType.Getter)]
public static class SpectatableOwnerlessGuardPatch
{
    [HarmonyPrefix]
    private static bool Prefix(SpectatableModuleBase __instance, ref ReferenceHub __result)
    {
        if (__instance == null) return true;
        if (__instance.MainRole.TryGetOwner(out ReferenceHub hub) && hub != null) return true;

        __result = null;

        return false;
    }
}
