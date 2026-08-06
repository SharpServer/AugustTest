using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

using Random = UnityEngine.Random;

namespace AntiMeme.Abilities;

/// <summary>
/// 周囲へ水しぶきを撒き、近くの敵を吹き飛ばしつつ足元を沈ませます。
/// </summary>
public sealed class AquaSplashAbility : AbilityBase
{
    private const float Radius = 6f;
    private const float KnockbackPower = 8.5f;
    private const float UpwardPower = 4f;
    private const float KnockbackDuration = 0.28f;
    private const byte SinkholeIntensity = 40;
    private const float SinkholeDuration = 8f;
    private const int RingSegments = 10;
    private const float RingLifetime = 0.4f;

    private static readonly Color WaterColor = new Color(0.25f, 0.85f, 1f, 0.45f);
    private static readonly Vector3 DropletScale = Vector3.one * 0.25f;

    /// <inheritdoc/>
    public override string Name => "アクアスプラッシュ";

    /// <inheritdoc/>
    public override string Description => "周囲に水しぶきを撒き散らし、近くの敵を吹き飛ばす。";

    /// <inheritdoc/>
    public override float Cooldown => 25f;

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        SpawnRing();

        foreach (Player target in AbilityTargeting.EnemiesAround(Player, Radius))
        {
            target.EnableEffect<Sinkhole>(SinkholeIntensity, SinkholeDuration);
            Knockback(target);
        }
    }

    private void Knockback(Player target)
    {
        Vector3 direction = target.Position - Player.Position;
        direction.y = 0f;

        // 真上から重なっていると向きが決まらないので、そのときだけ水平方向へ散らす。
        direction = direction.sqrMagnitude > 0.01f
            ? direction.normalized
            : Vector3.ProjectOnPlane(Random.insideUnitSphere, Vector3.up).normalized;

        FpcPush.Impulse(
            target,
            new Vector3(direction.x * KnockbackPower, UpwardPower, direction.z * KnockbackPower),
            KnockbackDuration);
    }

    private void SpawnRing()
    {
        Vector3 center = Player.Position + Vector3.up * 0.1f;

        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (Radius * 0.6f);

            AbilityVisuals.Spawn(PrimitiveType.Sphere, center + offset, DropletScale, WaterColor, RingLifetime);
        }
    }
}
