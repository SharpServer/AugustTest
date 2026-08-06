using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using HarmonyLib;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp173;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Patches;

/// <summary>
/// バニラ上は同じ SCP 陣営でも、こちらのチーム分けでは敵同士になる組み合わせ
/// (SCP 対 第五教会) を成立させます。
///
/// <para>
/// ゲーム側は「両方 <see cref="Team.SCPs"/> なら味方」と決め打ちしているため、
/// ダメージも SCP-173 のスナップも SCP-096 の攻撃も通りません。
/// 判定を挟む場所が 4 か所あるので、それぞれに当てています。
/// </para>
/// <para>
/// どのチームに属するかは<b>チーム自身が答える</b>ので、
/// 旧実装のような役職 ID → 陣営の対応表は要りません。
/// </para>
/// </summary>
internal static class ScpCrossFactionCombat
{
    /// <summary>
    /// バニラでは味方同士だが、こちらの陣営分けでは敵同士かどうか。
    /// </summary>
    internal static bool IsCrossFaction(ReferenceHub attackerHub, ReferenceHub victimHub)
    {
        if (attackerHub == null || victimHub == null || attackerHub == victimHub) return false;

        // バニラが味方と見なす組み合わせだけが対象。それ以外はゲーム側の判定で足りる。
        if (attackerHub.GetTeam() != Team.SCPs || victimHub.GetTeam() != Team.SCPs) return false;

        Player attacker = Player.Get(attackerHub);
        Player victim = Player.Get(victimHub);

        if (attacker is not { IsAlive: true } || victim is not { IsAlive: true }) return false;

        CustomTeam scps = CustomTeam.Get<ScpTeam>();
        CustomTeam fifthists = CustomTeam.Get<FifthistTeam>();

        return (scps.Includes(attacker) && fifthists.Includes(victim))
            || (fifthists.Includes(attacker) && scps.Includes(victim));
    }

    /// <summary>
    /// <see cref="HitboxIdentity.IsEnemy(RoleTypeId, RoleTypeId)"/> の差し替え先です。
    /// 役職だけでは陣営が決まらないので、加害者の実体を見てから判定します。
    /// </summary>
    internal static bool IsEnemyForDamage(
        RoleTypeId attackerRole,
        RoleTypeId victimRole,
        AttackerDamageHandler handler,
        ReferenceHub victimHub)
    {
        if (handler is not null && IsCrossFaction(handler.Attacker.Hub, victimHub)) return true;

        return HitboxIdentity.IsEnemy(attackerRole, victimRole);
    }

    /// <summary>
    /// カメラの Transform から持ち主を引きます。SCP-173 のスナップが原点しか渡してこないため。
    /// </summary>
    internal static bool TryGetOwnerFromCamera(Transform camera, out ReferenceHub ownerHub)
    {
        ownerHub = null;

        if (camera == null) return false;

        foreach (ReferenceHub hub in ReferenceHub.AllHubs)
        {
            if (hub == null || hub.PlayerCameraReference != camera) continue;

            ownerHub = hub;

            return true;
        }

        return false;
    }
}

/// <summary>
/// 当たり判定の敵味方判定に割り込みます。
/// </summary>
[HarmonyPatch(typeof(HitboxIdentity), nameof(HitboxIdentity.IsEnemy), typeof(ReferenceHub), typeof(ReferenceHub))]
public static class ScpCrossFactionHitboxEnemyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ReferenceHub attacker, ReferenceHub victim, ref bool __result)
    {
        if (!ScpCrossFactionCombat.IsCrossFaction(attacker, victim)) return true;

        __result = true;

        return false;
    }
}

/// <summary>
/// ダメージ処理内のフレンドリーファイア判定を、実体を見る版へ差し替えます。
/// </summary>
/// <remarks>
/// 役職 2 つしか受け取らない <see cref="HitboxIdentity.IsEnemy(RoleTypeId, RoleTypeId)"/> の呼び出しを、
/// ハンドラと被害者も渡す版に置き換えています。呼び出し先が見つからなければ何も変えずに
/// ログへ出します (黙って無効化しない)。
/// </remarks>
[HarmonyPatch(typeof(AttackerDamageHandler), nameof(AttackerDamageHandler.ProcessDamage))]
public static class ScpCrossFactionDamageFriendlyFirePatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ProcessDamageTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo original = AccessTools.Method(
            typeof(HitboxIdentity),
            nameof(HitboxIdentity.IsEnemy),
            [typeof(RoleTypeId), typeof(RoleTypeId)]);

        MethodInfo replacement = AccessTools.Method(
            typeof(ScpCrossFactionCombat),
            nameof(ScpCrossFactionCombat.IsEnemyForDamage));

        bool replaced = false;

        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                CodeInstruction loadHandler = new CodeInstruction(OpCodes.Ldarg_0);
                loadHandler.labels.AddRange(instruction.labels);
                instruction.labels.Clear();

                yield return loadHandler;
                yield return new CodeInstruction(OpCodes.Ldarg_1);

                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced = true;
            }

            yield return instruction;
        }

        if (!replaced)
        {
            Logger.Error(
                $"[AntiMeme] {nameof(AttackerDamageHandler)}.{nameof(AttackerDamageHandler.ProcessDamage)} の中に " +
                "IsEnemy(RoleTypeId, RoleTypeId) の呼び出しが見つからず、陣営越しの攻撃が有効になりませんでした。");
        }
    }
}

/// <summary>
/// SCP-173 のスナップが陣営越しの相手にも通るようにします。
/// </summary>
[HarmonyPatch(typeof(Scp173SnapAbility), nameof(Scp173SnapAbility.TryHitTarget))]
public static class Scp173CrossFactionSnapPatch
{
    private const float SnapRange = 1.5f;

    [HarmonyPostfix]
    private static void Postfix(Transform origin, ref ReferenceHub target, ref bool __result)
    {
        // バニラが当てられたなら触らない。取りこぼしたときだけ拾い直す。
        if (__result || origin == null) return;

        if (!ScpCrossFactionCombat.TryGetOwnerFromCamera(origin, out ReferenceHub ownerHub)) return;

        if (!TryGetHitbox(origin, out HitboxIdentity hitbox)) return;

        ReferenceHub targetHub = hitbox.TargetHub;

        if (!ScpCrossFactionCombat.IsCrossFaction(ownerHub, targetHub)) return;

        target = targetHub;
        __result = true;
    }

    private static bool TryGetHitbox(Transform origin, out HitboxIdentity hitbox)
    {
        hitbox = null;

        if (!Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, SnapRange, Scp173SnapAbility.SnapMask))
            return false;

        if (!hit.collider.TryGetComponent(out IDestructible destructible) || destructible is not HitboxIdentity found)
            return false;

        hitbox = found;

        return true;
    }
}

/// <summary>
/// SCP-096 の攻撃が陣営越しの相手にも通るようにします。
/// </summary>
[HarmonyPatch(typeof(Scp096HitHandler), nameof(Scp096HitHandler.ProcessHits))]
public static class Scp096CrossFactionHitPatch
{
    [HarmonyPostfix]
    private static void Postfix(Scp096HitHandler __instance, int count, ref Scp096HitResult __result)
    {
        if (__instance == null || __instance._scpRole == null) return;
        if (!__instance._scpRole.TryGetOwner(out ReferenceHub ownerHub)) return;

        Scp096HitResult extra = Scp096HitResult.None;
        HashSet<uint> processed = [];

        for (int i = 0; i < count && i < Scp096HitHandler.Hits.Length; i++)
        {
            Collider hit = Scp096HitHandler.Hits[i];

            if (hit == null || !hit.TryGetComponent(out IDestructible destructible) ||
                destructible is not HitboxIdentity hitbox)
                continue;

            ReferenceHub targetHub = hitbox.TargetHub;

            if (!ScpCrossFactionCombat.IsCrossFaction(ownerHub, targetHub)) continue;

            // 同じ相手の別部位が複数拾われるので、1 人 1 回に絞る。
            if (!processed.Add(hitbox.NetworkId)) continue;

            // 自分自身の当たり判定は遮蔽物から外して見通しを取る。
            int blockerMask = (int)Scp096HitHandler.SolidObjectMask & ~(1 << hit.gameObject.layer);

            if (Physics.Linecast(__instance._scpRole.CameraPosition, destructible.CenterOfMass, blockerMask))
                continue;

            bool isTarget = __instance._targetCounter.HasTarget(targetHub);
            float damage = isTarget ? __instance._humanTargetDamage : __instance._humanNontargetDamage;

            if (!__instance.DealDamage(hitbox, damage)) continue;

            ApplyHitEffect(__instance, targetHub, isTarget);

            extra |= Scp096HitResult.Human;

            if (!targetHub.IsAlive())
                extra |= Scp096HitResult.Lethal;
        }

        if (extra == Scp096HitResult.None) return;

        __instance.HitResult |= extra;
        __result |= extra;
    }

    private static void ApplyHitEffect(Scp096HitHandler handler, ReferenceHub targetHub, bool isTarget)
    {
        if (targetHub == null) return;

        switch (handler._damageType)
        {
            case Scp096DamageHandler.AttackType.SlapLeft:
            case Scp096DamageHandler.AttackType.SlapRight:
                targetHub.playerEffectsController.EnableEffect<Concussed>(2.5f);
                break;

            case Scp096DamageHandler.AttackType.Charge:
                targetHub.playerEffectsController.EnableEffect<Concussed>(isTarget ? 10f : 4f);
                break;
        }
    }
}
