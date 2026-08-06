using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Patches;

/// <summary>
/// サーバーが満員になったときにロビーのカウントダウンを飛ばすバニラ挙動を止めます。
///
/// <para>
/// <c>CharacterClassManager.Init</c> のコルーチンは
/// 「参加人数 &gt;= <c>ReservedMaxPlayers</c>」でロビー時間を打ち切ります。
/// このサーバーはロビー中に役職抽選とマップ生成を進めるので、
/// 打ち切られると準備が終わる前にラウンドが始まります。
/// 上限を実質無限にして、カウントダウンだけでラウンド開始を決めさせます。
/// </para>
/// </summary>
[HarmonyPatch]
internal static class FullServerRoundStartPatch
{
    private static readonly MethodInfo CapacityGetter =
        AccessTools.PropertyGetter(typeof(CustomNetworkManager), nameof(CustomNetworkManager.ReservedMaxPlayers));

    private static readonly MethodInfo CapacityReplacement =
        AccessTools.Method(typeof(FullServerRoundStartPatch), nameof(GetRoundStartCapacity));

    private static MethodBase TargetMethod() =>
        AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(CharacterClassManager), nameof(CharacterClassManager.Init)));

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool replaced = false;

        foreach (CodeInstruction instruction in instructions)
        {
            if (!replaced && instruction.Calls(CapacityGetter))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = CapacityReplacement;
                replaced = true;
            }

            yield return instruction;
        }

        if (!replaced)
            Logger.Error("[FullServerRoundStart] CharacterClassManager.Init の満員判定が見つからず、パッチが当たりませんでした。");
    }

    private static int GetRoundStartCapacity(CustomNetworkManager networkManager) => int.MaxValue;
}
