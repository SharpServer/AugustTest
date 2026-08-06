using AntiMeme.Effects;
using AntiMeme.Items.Bases;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// Copi-Cola。SCP-207 の見た目をした無害な炭酸で、飲むと 207 の副作用を洗い流します。
/// </summary>
public sealed class CopiCola : CustomUsable
{
    private const float ArtificialHealth = 25f;
    private const float ArtificialHealthDecay = 3.5f;
    private const float HealPerSecond = 5f;
    private const float HealDuration = 5f;
    private const byte DamageBonus = 25;
    private const float DamageBonusDuration = 10f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SCP207;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;

    /// <inheritdoc/>
    public override string Name => "Copi-Cola";

    /// <inheritdoc/>
    public override string Description => "「スッカリ冴える コピ・コーラ」";

    /// <inheritdoc/>
    protected override void OnUse(Player player)
    {
        player.DisableEffect<Scp207>();
        AddArtificialHealth(player, ArtificialHealth, ArtificialHealthDecay);
        Regeneration.Apply(player, HealPerSecond, HealDuration);
        player.EnableEffect<DamageBoost>(DamageBonus, DamageBonusDuration);
    }
}
