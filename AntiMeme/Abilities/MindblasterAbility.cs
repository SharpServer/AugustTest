using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// 視線方向へ弾体スキマティックを撃ち出し、当たった敵を焼きます。
/// </summary>
public sealed class MindblasterAbility : AbilityBase
{
    private const string SchematicName = "SCP3005";
    private const float FlightTime = 0.8f;
    private const float FlightDistance = 25f;
    private const float HitRadius = 1f;
    private const float HitDamage = 30f;
    private const float EffectDuration = 35f;

    /// <inheritdoc/>
    public override string Name => "思壊";

    /// <inheritdoc/>
    public override string Description => "第五的な力の塊を撃ち出し、直撃した相手を焼き尽くす。";

    /// <inheritdoc/>
    public override float Cooldown => 5f;

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        Vector3 forward = Player.Camera.forward.normalized;
        Vector3 start = Player.Position + Vector3.up * 0.5f;

        if (!ObjectSpawner.TrySpawnSchematic(SchematicName, start, Quaternion.LookRotation(forward), out SchematicObject missile))
            return;

        Vector3 end = start + forward * FlightDistance + Vector3.up * 0.15f;

        Scope.Track(Timing.RunCoroutine(Fly(missile, start, end)));
    }

    private IEnumerator<float> Fly(SchematicObject missile, Vector3 start, Vector3 end)
    {
        // 1 人につき 1 回だけ当てる。旧実装は毎フレーム判定していて、
        // 弾体の中に立ち止まると 30 ダメージが連続で入っていた。
        HashSet<Player> hit = [];

        float elapsed = 0f;

        while (elapsed < FlightTime)
        {
            // SchematicObject は MonoBehaviour なので Unity の破棄判定に乗せる (is null では見抜けない)。
            if (missile == null) yield break;

            Vector3 position = Vector3.Lerp(start, end, elapsed / FlightTime);
            missile.Position = position;

            foreach (Player target in AbilityTargeting.EnemiesAround(Player, position, HitRadius))
            {
                if (!hit.Add(target)) continue;

                target.EnableEffect<Burned>(255, EffectDuration);
                target.EnableEffect<Concussed>(255, EffectDuration);
                target.EnableEffect<Asphyxiated>(1, EffectDuration);
                target.Damage(HitDamage, Player, (end - start).normalized * HitDamage);

                Player.SendHitMarker();
            }

            elapsed += Time.deltaTime;

            yield return Timing.WaitForOneFrame;
        }

        if (missile != null)
            missile.Destroy();
    }
}
