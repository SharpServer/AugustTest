using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Effects;

/// <summary>
/// 一定間隔で <see cref="TickingEffectBase.OnTick"/> が回るカスタム状態効果の基底です。
///
/// 間隔は <see cref="TickRate"/> を override して決めます。ゲーム本体の
/// <see cref="TickingEffectBase.TimeBetweenTicks"/> を直接書き換える必要はありません。
/// </summary>
public abstract class CustomTickingEffect : TickingEffectBase, ICustomEffect
{
    /// <summary>1 フレームより短い間隔は意味がないので、ここで打ち止めにします。</summary>
    private const float MinimumTickRate = 0.01f;

    /// <summary>
    /// この効果が掛かっているプレイヤーです。効果が切れているあいだは null。
    /// </summary>
    public Player Player { get; private set; }

    /// <summary><see cref="TickingEffectBase.OnTick"/> の間隔 (秒)。</summary>
    protected virtual float TickRate => 1f;

    /// <inheritdoc/>
    public override void Enabled()
    {
        TimeBetweenTicks = Mathf.Max(MinimumTickRate, TickRate);

        // base.Enabled() が _timeTillTick に TimeBetweenTicks を入れるので、先に間隔を決めておく。
        base.Enabled();

        Player = Player.Get(Hub);
    }

    /// <inheritdoc/>
    public override void Disabled()
    {
        base.Disabled();
        Player = null;
    }

    /// <summary>
    /// 退出などで <see cref="Disabled"/> を通らずに破棄されたときの受け皿です。
    /// </summary>
    public virtual void OnDestroy() => Player = null;
}
