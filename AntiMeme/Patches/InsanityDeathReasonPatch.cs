using AntiMeme.Effects;
using HarmonyLib;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using PlayerStatsSystem;

namespace AntiMeme.Patches;

/// <summary>
/// 発狂状態 (<see cref="Insanity"/>) のまま死んだプレイヤーの死因表示を、
/// 実際の死因に関係なく発狂的な死に方テキストへ差し替えます。
///
/// <para>
/// <b>差し替えるのは表示だけです。</b> <c>PlayerStats.KillPlayer</c> に渡された
/// <see cref="DamageHandlerBase"/> 自体は書き換えないので、
/// キル数集計や勝利判定が受け取る加害者情報はそのまま残ります。
/// </para>
/// <para>
/// <c>KillPlayer</c> は同期実行で、内部の <see cref="RagdollManager.ServerSpawnRagdoll"/> と
/// <see cref="SpectatorRole.ServerSetData"/> もその中で完結します。
/// そのため「いま死んでいるプレイヤー」を 1 スロット持てば足ります。
/// </para>
/// </summary>
[HarmonyPatch]
internal static class InsanityDeathReasonPatch
{
    private static ReferenceHub dyingHub;
    private static string deathReason;

    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.KillPlayer), typeof(DamageHandlerBase))]
    [HarmonyPrefix]
    private static void CaptureInsaneDeath(PlayerStats __instance)
    {
        ReferenceHub hub = __instance._hub;

        if (hub == null || !IsInsane(hub)) return;

        dyingHub = hub;
        deathReason = Insanity.PickDeathReason();
    }

    [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.KillPlayer), typeof(DamageHandlerBase))]
    [HarmonyFinalizer]
    private static void ClearInsaneDeath()
    {
        dyingHub = null;
        deathReason = null;
    }

    /// <summary>死体を調べたときに出る死因です。</summary>
    [HarmonyPatch(typeof(RagdollManager), nameof(RagdollManager.ServerSpawnRagdoll))]
    [HarmonyPrefix]
    private static void OverrideRagdollReason(ReferenceHub owner, ref DamageHandlerBase handler)
    {
        if (TryGetReason(owner, out string reason))
            handler = new CustomReasonDamageHandler(reason);
    }

    /// <summary>死んだ本人のデスクリーンに出る死因です。</summary>
    [HarmonyPatch(typeof(SpectatorRole), nameof(SpectatorRole.ServerSetData))]
    [HarmonyPrefix]
    private static void OverrideDeathScreenReason(SpectatorRole __instance, ref DamageHandlerBase dhb)
    {
        if (!__instance.TryGetOwner(out ReferenceHub hub)) return;

        if (TryGetReason(hub, out string reason))
            dhb = new CustomReasonDamageHandler(reason);
    }

    private static bool TryGetReason(ReferenceHub hub, out string reason)
    {
        if (hub != null && hub == dyingHub && !string.IsNullOrEmpty(deathReason))
        {
            reason = deathReason;

            return true;
        }

        reason = string.Empty;

        return false;
    }

    private static bool IsInsane(ReferenceHub hub) =>
        hub.playerEffectsController != null &&
        hub.playerEffectsController.TryGetEffect(out Insanity insanity) &&
        insanity.IsEnabled;
}
