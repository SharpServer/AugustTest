using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;

namespace AntiMeme.Effects;

/// <summary>
/// 与ダメージに <see cref="CustomPlayerEffects.StatusEffectBase.Intensity"/> ぶんを上乗せします。
/// </summary>
public class DamageBoost : CustomEffect, ICustomDisplayName
{
    /// <inheritdoc/>
    public bool CanBeDisplayed => true;

    /// <inheritdoc/>
    public string DisplayName => "Damage Boost";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Positive;

    /// <inheritdoc/>
    protected override void OnEffectEnabled() =>
        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!IsMine(ev.Attacker) || ev.DamageHandler is not StandardDamageHandler damage) return;

        damage.Damage += Intensity;
    }
}
