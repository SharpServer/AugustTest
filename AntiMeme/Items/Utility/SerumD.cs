using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// Serum-D。SCP-1853 の性質を借りて、短時間だけ器用さを底上げするセラムです。
/// アドレナリンとしての効果もそのまま出ます。
/// </summary>
public sealed class SerumD : CustomUsable, IScp914Upgradable
{
    private const byte DexterityIntensity = 4;
    private const float Duration = 15f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Adrenaline;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;

    /// <inheritdoc/>
    public override string Name => "Serum-D";

    /// <inheritdoc/>
    public override string Description =>
        "SCP-1853の性質を参考に開発された強化用セラム。\n短時間、器用さを大幅に向上させる";

    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.Adrenaline),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.To<SerumC>(),
        VeryFine = Scp914Rule.To<SerumC>(),
    };

    /// <inheritdoc/>
    protected override bool CancelVanillaUse => false;

    /// <inheritdoc/>
    protected override void OnUse(Player player) =>
        player.EnableEffect<Scp1853>(DexterityIntensity, Duration);
}
