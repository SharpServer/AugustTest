using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles.PlayableScps.Scp049;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// SCP-049 の「名医の感」を一定時間だけ全方位へ拡張します。
/// </summary>
public sealed class SenseOfGreatDoctorAbility : AbilityBase
{
    private const float Duration = 60f;
    private const float Interval = 0.5f;
    private const float Range = 35f;
    private const float SenseLockout = 120f;
    private const byte SpeedBoost = 10;

    /// <inheritdoc/>
    public override string Name => "名医の感";

    /// <inheritdoc/>
    public override string Description => "一定時間、周囲の人間すべてを見通す。";

    /// <inheritdoc/>
    public override float Cooldown => 200f;

    /// <inheritdoc/>
    protected override bool CanUse(out string failureReason)
    {
        if (!base.CanUse(out failureReason)) return false;

        if (!TryGetSense(out Scp049SenseAbility sense))
        {
            failureReason = "SCP-049でなければ使用できません。";

            return false;
        }

        if (sense.Duration.Remaining > 0f || sense.Cooldown.Remaining > 0f)
        {
            failureReason = "名医の感は現在使用できません。";

            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        if (!TryGetSense(out Scp049SenseAbility sense)) return;

        // 効果中はバニラの感を封じる。クールダウンはクライアントへ同期しないと HUD がずれる。
        sense.Cooldown.Remaining = SenseLockout;
        sense.ServerSendRpc(true);

        // 旧実装は 0.1 秒ごとに 1 秒の MovementBoost を掛け直していたが、一度に掛ければ同じ。
        Player.EnableEffect<MovementBoost>(SpeedBoost, Duration);

        Scope.Track(Timing.RunCoroutine(Reveal()));
    }

    private IEnumerator<float> Reveal()
    {
        float end = Time.time + Duration;

        while (Time.time < end)
        {
            if (!TryGetSense(out _)) yield break;

            foreach (Player target in AbilityTargeting.Around(Player, Range))
            {
                if (target.IsSCP) continue;

                // 次の周回まで持つ長さで掛け直す。範囲から出た相手は自然に消える。
                target.EnableEffect<AnomalousTarget>(1, Interval * 2f);
            }

            yield return Timing.WaitForSeconds(Interval);
        }
    }

    private bool TryGetSense(out Scp049SenseAbility sense)
    {
        if (Player.RoleBase is Scp049Role role)
            return role.SubroutineModule.TryGetSubroutine(out sense);

        sense = null;

        return false;
    }
}
