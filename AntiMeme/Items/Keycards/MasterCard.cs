using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// ただの MasterCard です。権限は持ちません。
/// </summary>
public sealed class MasterCard : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<Utility.Quarter>(8),
        OneToOne = Scp914Rule.To<PlayingCard>(),
        Fine = Scp914Rule.Weighted(
            (0.4f, Scp914Rule.ToVanilla(ItemType.KeycardScientist)),
            (0.3f, Scp914Rule.To<KeycardSiteNavigator>()),
            (0.3f, Scp914Rule.To<KeycardArmoryLevel2>())),
        VeryFine = VanillaScp914Rules.WithO5(
            0.10f,
            (0.25f, Scp914Rule.To<KeycardSiteDirector>()),
            (0.15f, Scp914Rule.ToVanilla(ItemType.KeycardFacilityManager)),
            (0.10f, Scp914Rule.ToVanilla(ItemType.KeycardJanitor)),
            (0.40f, Scp914Rule.Destroy)),
    };

    /// <inheritdoc/>
    public override string Name => "MasterCard";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.blue;

    /// <inheritdoc/>
    public override string Description => "ただのMasterCard。何にも使えない";

    /// <inheritdoc/>
    protected override string Label => "MasterCard";

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(0, 56, 170, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(0, 0, 0, 255);

    /// <inheritdoc/>
    protected override string HolderName => "MasterCard";
}
