using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using RemoteAdmin.Interfaces;

namespace AntiMeme.Effects;

/// <summary>
/// SCP-106 の <see cref="Traumatized"/> を「見た目だけ」流用します。
/// ゲーム側の付随デバフは Harmony パッチで打ち消してもらいます
/// (<see cref="TryGetOwner"/> がその判定の入口です)。
///
/// 本物の <see cref="Traumatized"/> が既に掛かっているときは手を出しません。
/// 後から解除して本物まで消してしまうのを避けるためで、
/// 「自分が点けたぶんだけ自分で消す」を <see cref="ownsState"/> で持ちます。
/// 作りは <see cref="VisualSinkhole"/> と揃えてあります。
/// </summary>
public class VisualTraumatized : CustomEffect, ICustomDisplayName
{
    private bool blockedByReal;
    private bool ownsState;

    /// <inheritdoc/>
    public bool CanBeDisplayed => false;

    /// <inheritdoc/>
    public string DisplayName => "Visual Traumatized";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Technical;

    /// <summary>
    /// 指定プレイヤーの <see cref="Traumatized"/> を、この効果が握っているかどうか。
    /// Harmony パッチから「付随デバフを打ち消してよいか」を判定するために使います。
    /// </summary>
    public static bool TryGetOwner(ReferenceHub hub, out VisualTraumatized effect)
    {
        effect = null;

        return hub != null &&
               hub.playerEffectsController != null &&
               hub.playerEffectsController.TryGetEffect(out effect) &&
               effect.IsEnabled &&
               effect.ownsState;
    }

    /// <summary>
    /// SCP-106 の即死判定を抑えるべきかどうか。
    ///
    /// ゲーム側は「対象が <see cref="Traumatized"/> を持っているか」で
    /// ポケット次元送りにするかを決めます。こちらが見た目のために点けたぶんで
    /// 即死させてしまわないよう、Harmony パッチからこれを見て打ち消します。
    /// </summary>
    public static bool ShouldSuppressScp106Kill(ReferenceHub hub) => TryGetOwner(hub, out _);

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
        blockedByReal = Player.GetEffect<Traumatized>() is { IsEnabled: true };
        Apply();
    }

    /// <inheritdoc/>
    protected override void OnEffectDisabled()
    {
        // Duration が 0 のもの (= こちらが点けた無期限の分) だけ落とす。
        if (ownsState && Player.GetEffect<Traumatized>() is { IsEnabled: true, Duration: 0f } traumatized)
            traumatized.ServerSetState(0);

        ownsState = false;
        blockedByReal = false;
    }

    private void Apply()
    {
        if (blockedByReal || !IsEnabled || Player is not { IsDestroyed: false }) return;

        if (Player.GetEffect<Traumatized>() is not { } traumatized) return;

        if (traumatized.IsEnabled)
        {
            // こちらが点けたものでなければ触らない。
            if (!ownsState)
                blockedByReal = true;

            return;
        }

        traumatized.ServerSetState(1);
        ownsState = true;
    }
}
