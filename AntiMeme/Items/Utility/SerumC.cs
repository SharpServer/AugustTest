using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// Serum-C。<see cref="SerumD"/> の上位版で、器用さに加えて足の速さも上がります。
/// </summary>
public sealed class SerumC : CustomUsable, IScp914Upgradable
{
    private const byte DexterityIntensity = 4;
    private const byte SpeedIntensity = 15;
    private const float Duration = 30f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Adrenaline;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;

    /// <inheritdoc/>
    public override string Name => "Serum-C";

    /// <inheritdoc/>
    public override string Description =>
        "Serum-Dを元に開発された上級のセラム。\n短時間、器用さと早さを大幅に向上させる";

    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<SerumD>(),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.Keep,
        VeryFine = Scp914Rule.Keep,
    };

    /// <inheritdoc/>
    protected override bool CancelVanillaUse => false;

    /// <inheritdoc/>
    protected override void OnUse(Player player)
    {
        player.EnableEffect<Scp1853>(DexterityIntensity, Duration);
        player.EnableEffect<MovementBoost>(SpeedIntensity, Duration);
    }
}
