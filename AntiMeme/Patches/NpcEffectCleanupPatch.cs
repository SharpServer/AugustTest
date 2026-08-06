using System.Collections.Generic;
using CustomPlayerEffects;
using HarmonyLib;
using MEC;
using Sliced.API.Features;

using Npc = Exiled.API.Features.Npc;

namespace AntiMeme.Patches;

/// <summary>
/// NPC を消す前に、効果の解除がクライアントへ届く時間を作ります。
///
/// <para>
/// 効果の状態は所有者の <c>NetworkIdentity</c> にぶら下がって同期されます。
/// 効果が付いたまま NPC を破棄すると解除が届かず、
/// クライアント側に効果の画面演出 (暗転・耳鳴りなど) が残り続けます。
/// 破棄要求が来た時点で効果を落とし、
/// <see cref="DestroyDelay"/> だけ待ってから実際に破棄します。
/// </para>
/// <para>
/// 待っている間に効果を付け直されると元の木阿弥なので、
/// その間だけ効果の有効化を止めます (解除は通します)。
/// </para>
/// </summary>
public static class NpcEffectCleanup
{
    /// <summary>効果の解除がクライアントへ届くまでの待ち時間です。</summary>
    public const float DestroyDelay = 0.25f;

    private static readonly HashSet<ReferenceHub> Pending = new HashSet<ReferenceHub>();
    private static readonly HashSet<ReferenceHub> Ready = new HashSet<ReferenceHub>();

    /// <summary>
    /// この NPC が破棄待ちかどうか。
    /// </summary>
    public static bool IsPending(ReferenceHub hub) => hub is not null && Pending.Contains(hub);

    /// <summary>
    /// 破棄を受け付けます。すぐ破棄してよければ true、待たせる場合は false を返します。
    /// </summary>
    public static bool BeginDestroy(Npc npc)
    {
        ReferenceHub hub = npc.ReferenceHub;

        if (hub == null) return true;

        // 待ち時間を終えて自分で呼び直した分はそのまま通す。
        if (Ready.Remove(hub))
        {
            Pending.Remove(hub);

            return true;
        }

        if (!Pending.Add(hub)) return false;

        npc.DisableAllEffects();

        // ラウンド再開で握り潰されると NPC が残るので、ここはスコープに載せない。
        Timing.CallDelayed(DestroyDelay, () =>
        {
            if (!Pending.Remove(hub)) return;

            if (hub == null || Npc.Get(hub) is not { } current)
            {
                Ready.Remove(hub);

                return;
            }

            Ready.Add(hub);
            current.Destroy();
        });

        return false;
    }

    internal static void Reset()
    {
        Pending.Clear();
        Ready.Clear();
    }
}

[HarmonyPatch(typeof(Npc), nameof(Npc.Destroy))]
public static class NpcDestroyEffectCleanupPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Npc __instance) => __instance is null || NpcEffectCleanup.BeginDestroy(__instance);
}

[HarmonyPatch(typeof(StatusEffectBase), nameof(StatusEffectBase.ServerSetState))]
public static class NpcPendingDestroyEffectPatch
{
    [HarmonyPrefix]
    private static bool Prefix(StatusEffectBase __instance, byte intensity)
    {
        // 解除 (intensity 0) は常に通す。止めるのは破棄待ちの NPC への付与だけ。
        if (intensity == 0 || __instance is null) return true;

        return !NpcEffectCleanup.IsPending(__instance.Hub);
    }
}

/// <summary>
/// ラウンド再開で破棄待ちの記録を捨てます。
/// </summary>
public sealed class NpcEffectCleanupHandler : EventHandlerBase
{
    public override void OnServerRoundRestarted() => NpcEffectCleanup.Reset();
}
