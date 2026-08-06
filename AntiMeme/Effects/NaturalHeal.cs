using RemoteAdmin.Interfaces;
using UnityEngine;

namespace AntiMeme.Effects;

/// <summary>
/// 0.1 秒ごとに <see cref="CustomPlayerEffects.StatusEffectBase.Intensity"/> ぶんだけ回復します。
/// </summary>
public class NaturalHeal : CustomTickingEffect, ICustomDisplayName
{
    /// <inheritdoc/>
    public bool CanBeDisplayed => true;

    /// <inheritdoc/>
    public string DisplayName => "Natural Heal";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Positive;

    /// <inheritdoc/>
    protected override float TickRate => 0.1f;

    /// <inheritdoc/>
    public override void OnTick()
    {
        if (Player is not { IsDestroyed: false, IsAlive: true } || Intensity == 0) return;

        Player.Health = Mathf.Min(Player.MaxHealth, Player.Health + Intensity);
    }
}
