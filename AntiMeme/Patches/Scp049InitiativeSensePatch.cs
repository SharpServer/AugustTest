using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AntiMeme.Teams.Factions;
using HarmonyLib;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps;
using PlayerRoles.PlayableScps.Scp049;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;
using AntiMeme.Roles.Initiative;

namespace AntiMeme.Patches;

/// <summary>
/// Initiative Wolf の SCP-049 sense を SCP 陣営向けに反転します。
/// ゲーム本体が command 検証と追跡中の双方で固定の敵味方判定を行うため、
/// その 2 呼び出しだけを役職対応版へ差し替えます。
/// </summary>
[HarmonyPatch(typeof(Scp049SenseAbility))]
public static class Scp049InitiativeSensePatch
{
    private const float IndicatorRefreshInterval = 3f;
    private static readonly Dictionary<Scp049SenseAbility, float> NextIndicatorRefresh = [];

    [HarmonyPatch(nameof(Scp049SenseAbility.ServerProcessCmd), typeof(NetworkReader))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ServerProcessCmdTranspiler(
        IEnumerable<CodeInstruction> instructions) =>
        ReplaceEnemyCheck(instructions, nameof(Scp049SenseAbility.ServerProcessCmd));

    [HarmonyPatch(nameof(Scp049SenseAbility.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UpdateTranspiler(
        IEnumerable<CodeInstruction> instructions) =>
        ReplaceEnemyCheck(instructions, nameof(Scp049SenseAbility.Update));

    [HarmonyPatch(nameof(Scp049SenseAbility.Update))]
    [HarmonyPostfix]
    private static void UpdatePostfix(Scp049SenseAbility __instance)
    {
        if (!NetworkServer.active || !__instance.HasTarget ||
            __instance.Target == null || __instance.Duration.IsReady)
        {
            NextIndicatorRefresh.Remove(__instance);
            return;
        }

        float now = Time.realtimeSinceStartup;
        if (!NextIndicatorRefresh.TryGetValue(__instance, out float nextRefresh))
        {
            NextIndicatorRefresh[__instance] = now + IndicatorRefreshInterval;
            return;
        }

        if (now < nextRefresh) return;

        __instance.ServerSendRpc(toAll: true);
        NextIndicatorRefresh[__instance] = now + IndicatorRefreshInterval;
    }

    [HarmonyPatch(nameof(Scp049SenseAbility.ResetObject))]
    [HarmonyPostfix]
    private static void ResetObjectPostfix(Scp049SenseAbility __instance) =>
        NextIndicatorRefresh.Remove(__instance);

    internal static void Clear() => NextIndicatorRefresh.Clear();

    internal static bool TryFindInitiativeTarget(Scp049SenseAbility ability, out Player target)
    {
        target = null;
        if (ability?.Owner == null) return false;

        Player owner = Player.Get(ability.Owner);
        if (owner == null || CustomRole.Of(owner) is not InitiativeWolf) return false;

        Transform camera = ability.Owner.PlayerCameraReference;
        if (camera == null) return false;

        float maximumDistanceSqr = ability._distanceThreshold * ability._distanceThreshold;
        float minimumDot = ability._dotThreshold;
        Vector3 ownerPosition = ability.CastRole.FpcModule.Position;
        CustomTeam scpTeam = CustomTeam.Get<ScpTeam>();

        foreach (Player candidate in Player.ReadyList)
        {
            if (candidate == owner || !candidate.IsAlive || !scpTeam.Includes(candidate) ||
                candidate.ReferenceHub.roleManager.CurrentRole is not FpcStandardRoleBase candidateRole)
            {
                continue;
            }

            Vector3 candidatePosition = candidateRole.FpcModule.Position;
            Vector3 direction = candidatePosition - camera.position;
            Vector3 forward = camera.forward;

            if (Mathf.Abs((candidatePosition - ownerPosition).y) < Scp049SenseAbility.HeightDiffIgnoreY &&
                direction.sqrMagnitude < Scp049SenseAbility.NearbyDistanceSqr)
            {
                forward.y = 0f;
                forward.Normalize();
                direction.y = 0f;
            }

            float dot = Vector3.Dot(forward, direction.normalized);
            float distanceSqr = (candidatePosition - ownerPosition).sqrMagnitude;
            if (dot < minimumDot || distanceSqr > maximumDistanceSqr) continue;

            float radius = candidateRole.FpcModule.CharacterControllerSettings.Radius;
            if (!VisionInformation.GetVisionInformation(
                    ability.Owner,
                    camera,
                    candidateRole.CameraPosition,
                    radius,
                    ability._distanceThreshold,
                    checkFog: true,
                    checkLineOfSight: true,
                    maskLayer: 0,
                    checkInDarkness: false)
                .IsLooking)
            {
                continue;
            }

            maximumDistanceSqr = distanceSqr;
            minimumDot = dot;
            target = candidate;
        }

        return target != null;
    }

    public static bool IsInitiativeSenseTargetAllowed(ReferenceHub ownerHub, ReferenceHub targetHub)
    {
        Player owner = Player.Get(ownerHub);
        if (owner == null || CustomRole.Of(owner) is not InitiativeWolf)
            return HitboxIdentity.IsEnemy(ownerHub, targetHub);

        Player target = Player.Get(targetHub);
        return target != null && targetHub != ownerHub && target.IsAlive &&
               CustomTeam.Get<ScpTeam>().Includes(target);
    }

    private static IEnumerable<CodeInstruction> ReplaceEnemyCheck(
        IEnumerable<CodeInstruction> instructions,
        string targetMethod)
    {
        MethodInfo original = AccessTools.Method(
            typeof(HitboxIdentity),
            nameof(HitboxIdentity.IsEnemy),
            [typeof(ReferenceHub), typeof(ReferenceHub)]);
        MethodInfo replacement = AccessTools.Method(
            typeof(Scp049InitiativeSensePatch),
            nameof(IsInitiativeSenseTargetAllowed));
        bool replaced = false;

        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced = true;
            }

            yield return instruction;
        }

        if (!replaced)
            Logger.Error($"[Scp049InitiativeSensePatch] IsEnemy was not found in {targetMethod}; patch skipped.");
    }
}

[HarmonyPatch(typeof(Scp049AttackAbility), nameof(Scp049AttackAbility.IsTargetValid))]
public static class Scp049InitiativeAttackPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> IsTargetValidTranspiler(
        IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo original = AccessTools.Method(
            typeof(HitboxIdentity),
            nameof(HitboxIdentity.IsEnemy),
            [typeof(ReferenceHub), typeof(ReferenceHub)]);
        MethodInfo replacement = AccessTools.Method(
            typeof(Scp049InitiativeAttackPatch),
            nameof(IsInitiativeAttackTargetAllowed));
        bool replaced = false;

        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(original))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced = true;
            }

            yield return instruction;
        }

        if (!replaced)
            Logger.Error("[Scp049InitiativeAttackPatch] IsEnemy was not found in IsTargetValid; patch skipped.");
    }

    public static bool IsInitiativeAttackTargetAllowed(ReferenceHub ownerHub, ReferenceHub targetHub)
    {
        Player owner = Player.Get(ownerHub);
        if (owner == null || CustomRole.Of(owner) is not InitiativeWolf)
            return HitboxIdentity.IsEnemy(ownerHub, targetHub);

        Player target = Player.Get(targetHub);
        return target != null && targetHub != ownerHub && target.IsAlive &&
               !CustomTeam.Get<InitiativeTeam>().Includes(target);
    }
}

/// <summary>LabAPI の可変 sense ターゲットとパッチ内キャッシュの寿命を所有します。</summary>
public sealed class Scp049InitiativeSenseHandler : EventHandlerBase
{
    public override void RegisterEvents() => Scp049Events.UsingSense += OnUsingSense;

    public override void UnregisterEvents() => Scp049Events.UsingSense -= OnUsingSense;

    protected override void OnDisabled() => Scp049InitiativeSensePatch.Clear();

    private static void OnUsingSense(Scp049UsingSenseEventArgs ev)
    {
        if (ev.Player == null || CustomRole.Of(ev.Player) is not InitiativeWolf ||
            ev.Player.RoleBase is not Scp049Role role ||
            !role.SubroutineModule.TryGetSubroutine(out Scp049SenseAbility sense))
        {
            return;
        }

        ev.Target = Scp049InitiativeSensePatch.TryFindInitiativeTarget(sense, out Player target)
            ? target
            : null;
        ev.IsAllowed = ev.Target != null;
    }
}
