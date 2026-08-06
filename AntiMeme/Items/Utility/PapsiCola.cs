using AntiMeme.Effects;
using AntiMeme.Items.Bases;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// Papsi Cola。SCP-207 の解毒剤の見た目をした炭酸で、飲むと解毒剤の効果を打ち消します。
/// </summary>
public sealed class PapsiCola : CustomUsable
{
    private const float ArtificialHealth = 25f;
    private const float ArtificialHealthDecay = 3.5f;
    private const float HealPerSecond = 5f;
    private const float HealDuration = 5f;
    private const byte DamageBonus = 25;
    private const float DamageBonusDuration = 10f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.AntiSCP207;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;

    /// <inheritdoc/>
    public override string Name => "Papsi Cola";

    /// <inheritdoc/>
    public override string Description => "「パプシの力で生き生きと」";

    /// <inheritdoc/>
    protected override void OnUse(Player player)
    {
        player.DisableEffect<AntiScp207>();
        AddArtificialHealth(player, ArtificialHealth, ArtificialHealthDecay);
        Regeneration.Apply(player, HealPerSecond, HealDuration);
        player.EnableEffect<DamageBoost>(DamageBonus, DamageBonusDuration);
    }
}
