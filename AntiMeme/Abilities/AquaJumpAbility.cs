using Sliced.API.Features;
using UnityEngine;

using Random = UnityEngine.Random;

namespace AntiMeme.Abilities;

/// <summary>
/// 足元へ水を噴射して高く跳びます。滞空中だけ重力が弱まります。
/// </summary>
public sealed class AquaJumpAbility : AbilityBase
{
    private const float JumpPower = 8.75f;
    private const float ForwardBoost = 6f;
    private const float ForwardDuration = 0.2f;
    private const int JetCount = 6;
    private const float JetLifetime = 0.35f;
    private const float JetSpread = 0.3f;

    private static readonly Color WaterColor = new Color(0.25f, 0.85f, 1f, 0.5f);
    private static readonly Vector3 JetScale = new Vector3(0.15f, 0.6f, 0.15f);
    private static readonly Vector3 JetGravity = new Vector3(0f, -4.5f, 0f);

    /// <inheritdoc/>
    public override string Name => "アクアジェットジャンプ";

    /// <inheritdoc/>
    public override string Description => "足元へ水を噴射し、向いている方向へ大きく跳ぶ。";

    /// <inheritdoc/>
    public override float Cooldown => 12f;

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        SpawnJets();

        Vector3 forward = Vector3.ProjectOnPlane(Player.Camera.forward, Vector3.up).normalized * ForwardBoost;

        if (FpcPush.Jump(Player, JumpPower, forward, JetGravity, JetLifetime))
            FpcPush.Impulse(Player, forward, ForwardDuration);
    }

    private void SpawnJets()
    {
        Vector3 origin = Player.Position + Vector3.down * 0.05f;

        for (int i = 0; i < JetCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-JetSpread, JetSpread),
                0f,
                Random.Range(-JetSpread, JetSpread));

            AbilityVisuals.Spawn(PrimitiveType.Cylinder, origin + offset, JetScale, WaterColor, JetLifetime);
        }
    }
}
