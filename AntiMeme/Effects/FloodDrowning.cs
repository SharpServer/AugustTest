using CustomPlayerEffects;
using RemoteAdmin.Interfaces;

namespace AntiMeme.Effects;

/// <summary>
/// 浸水に飲まれた状態。視界と足を奪いながら継続ダメージを与え、最後の一撃で確実に殺します。
///
/// 見た目のエフェクトは 1.1 秒ぶんずつ掛け直します (tick が 0.35 秒なので途切れません)。
/// 旧実装は毎フレームこの貼り直しを走らせていましたが、tick と有効時間の差で足りるため
/// フレーム単位のリフレッシュは持ち込んでいません。
/// </summary>
public class FloodDrowning : CustomTickingEffect, ICustomDisplayName
{
    /// <summary>浸水イベント側が使う既定の強度です。</summary>
    public const byte DefaultIntensity = 255;

    /// <summary>浸水イベント側が使う既定の持続時間です。</summary>
    public const float DefaultDuration = 6f;

    private const float DamagePerTick = 28f;
    private const float FinishingDamage = 5000f;
    private const float VisualRefreshDuration = 1.1f;
    private const string DeathText = "溺死した";

    /// <inheritdoc/>
    public bool CanBeDisplayed => true;

    /// <inheritdoc/>
    public string DisplayName => "Flood Drowning";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Negative;

    /// <inheritdoc/>
    protected override float TickRate => 0.35f;

    /// <inheritdoc/>
    public override void Enabled()
    {
        base.Enabled();
        ApplyVisuals();
    }

    /// <inheritdoc/>
    public override void IntensityChanged(byte prevState, byte newState)
    {
        base.IntensityChanged(prevState, newState);

        if (newState > 0)
            ApplyVisuals();
    }

    /// <inheritdoc/>
    public override void OnTick()
    {
        if (Player is not { IsDestroyed: false, IsAlive: true } || Intensity == 0) return;

        ApplyVisuals();

        Player.Damage(Player.Health <= DamagePerTick ? FinishingDamage : DamagePerTick, DeathText);
    }

    private void ApplyVisuals()
    {
        if (Player is not { IsDestroyed: false, IsAlive: true } || Intensity == 0) return;

        Player.EnableEffect<Sinkhole>(255, VisualRefreshDuration);
        Player.EnableEffect<Slowness>(90, VisualRefreshDuration);
        Player.EnableEffect<Blindness>(80, VisualRefreshDuration);
        Player.EnableEffect<Blurred>(255, VisualRefreshDuration);
        Player.EnableEffect<Concussed>(255, VisualRefreshDuration);
        Player.EnableEffect<Deafened>(255, VisualRefreshDuration);
        Player.EnableEffect<Hemorrhage>(120, VisualRefreshDuration);
        Player.EnableEffect<VisualTraumatized>(180, VisualRefreshDuration);
    }
}
