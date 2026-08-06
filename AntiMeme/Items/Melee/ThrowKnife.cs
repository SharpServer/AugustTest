using System.Collections.Generic;
using AntiMeme.Abilities;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>
/// 投げナイフ。振ると刃が手を離れ、まっすぐ飛んでいきます。
/// </summary>
/// <remarks>
/// 旧実装は当たり判定で SCP を丸ごと除外していたので、SCP にはどれだけ当てても無傷でした。
/// また敵味方を見ずに人間全員を刺していました。ここでは
/// <see cref="AbilityTargeting.EnemiesAround"/> に判定を任せています。
/// </remarks>
public sealed class ThrowKnife : MeleeWeapon
{
    private const string SchematicName = "ThrowKnife";
    private const float FlightTime = 0.8f;
    private const float FlightDistance = 25f;
    private const float HitRadius = 1f;
    private const float HitDamage = 50f;
    private const float BleedDuration = 15f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SCP1509;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;

    /// <inheritdoc/>
    public override string Name => "投げナイフ";

    /// <inheritdoc/>
    public override string Description => "投擲用に重心を整えたナイフ。一投限り。";

    /// <inheritdoc/>
    protected override bool AllowResurrect => false;

    /// <summary>
    /// 振る操作をそのまま投擲に置き換えます。手元の刃は残りません。
    /// </summary>
    protected override bool OnAttacking(Player player)
    {
        Vector3 forward = player.Camera.forward.normalized;
        Vector3 start = player.Position + Vector3.up * 0.5f;

        if (!ObjectSpawner.TrySpawnSchematic(SchematicName, start, Quaternion.LookRotation(forward), out SchematicObject knife))
            return false;

        Vector3 end = start + forward * FlightDistance + Vector3.up * 0.15f;

        PlayerScope.Of(player).Track(Timing.RunCoroutine(Fly(player, knife, start, end)));

        Destroy();

        return false;
    }

    private static IEnumerator<float> Fly(Player thrower, SchematicObject knife, Vector3 start, Vector3 end)
    {
        // 1 人につき 1 回だけ刺さる。旧実装は刃の中に立ち止まると毎フレーム 50 ダメージが入っていた。
        HashSet<Player> hit = [];

        float elapsed = 0f;

        while (elapsed < FlightTime)
        {
            // SchematicObject は MonoBehaviour なので Unity の破棄判定に乗せる (is null では見抜けない)。
            if (knife == null) yield break;

            Vector3 position = Vector3.Lerp(start, end, elapsed / FlightTime);
            knife.Position = position;

            foreach (Player target in AbilityTargeting.EnemiesAround(thrower, position, HitRadius))
            {
                if (!hit.Add(target)) continue;

                target.EnableEffect<Bleeding>(20, BleedDuration);
                target.Damage(HitDamage, thrower, (end - start).normalized * HitDamage);

                thrower.SendHitMarker();
            }

            elapsed += Time.deltaTime;

            yield return Timing.WaitForOneFrame;
        }

        if (knife != null)
            knife.Destroy();
    }
}
