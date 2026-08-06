using CustomPlayerEffects;
using RemoteAdmin.Interfaces;

namespace AntiMeme.Effects;

/// <summary>
/// SCP-106 の <see cref="Sinkhole"/> を「見た目だけ」流用します。
/// 移動速度・スタミナ・足音の上書きは Harmony パッチ側で打ち消してもらいます
/// (<see cref="TryGetOwner"/> がその判定の入口です)。
///
/// 本物の <see cref="Sinkhole"/> が既に掛かっているときは手を出しません。
/// 後から解除して本物まで消してしまうのを避けるためで、
/// 「自分が点けたぶんだけ自分で消す」を <see cref="ownsState"/> で持ちます。
/// </summary>
public class VisualSinkhole : CustomEffect, ICustomDisplayName
{
    private bool blockedByReal;
    private bool ownsState;

    /// <inheritdoc/>
    public bool CanBeDisplayed => false;

    /// <inheritdoc/>
    public string DisplayName => "Visual Sinkhole";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Technical;

    /// <summary>
    /// この効果が <see cref="Sinkhole"/> を握っているあいだ、バニラの足音上書きを走らせるか。
    /// </summary>
    public bool FootstepOverridesEnabled { get; set; } = true;

    /// <summary>
    /// 指定プレイヤーの <see cref="Sinkhole"/> を、この効果が握っているかどうか。
    /// Harmony パッチから「移動デバフを打ち消してよいか」を判定するために使います。
    /// </summary>
    public static bool TryGetOwner(ReferenceHub hub, out VisualSinkhole effect)
    {
        effect = null;

        return hub != null &&
               hub.playerEffectsController != null &&
               hub.playerEffectsController.TryGetEffect(out effect) &&
               effect.IsEnabled &&
               effect.ownsState;
    }

    /// <inheritdoc/>
    public override void OnEffectUpdate()
    {
        base.OnEffectUpdate();
        Apply();
    }

    /// <inheritdoc/>
    public override void IntensityChanged(byte prevState, byte newState)
    {
        base.IntensityChanged(prevState, newState);

        if (newState > 0)
            Apply();
    }

    /// <inheritdoc/>
    protected override void OnEffectEnabled()
    {
        blockedByReal = Player.GetEffect<Sinkhole>() is { IsEnabled: true };
        Apply();
    }

    /// <inheritdoc/>
    protected override void OnEffectDisabled()
    {
        // Duration が 0 のもの (= こちらが点けた無期限の分) だけ落とす。
        if (ownsState && Player.GetEffect<Sinkhole>() is { IsEnabled: true, Duration: 0f } sinkhole)
            sinkhole.ServerSetState(0);

        ownsState = false;
        blockedByReal = false;
    }

    private void Apply()
    {
        if (Intensity == 0 || Player is not { IsDestroyed: false } || blockedByReal) return;

        if (Player.GetEffect<Sinkhole>() is not { } sinkhole) return;

        if (!ownsState)
        {
            if (sinkhole.IsEnabled)
            {
                blockedByReal = true;

                return;
            }

            ownsState = true;
        }

        if (!sinkhole.IsEnabled || sinkhole.Intensity != Intensity)
            sinkhole.ServerSetState(Intensity);
    }
}
