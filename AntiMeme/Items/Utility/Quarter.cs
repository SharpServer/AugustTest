using AntiMeme.Items.Scp914;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>Trashbox などで使われる 25 セント硬貨です。</summary>
public sealed class Quarter : CustomItem, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.Destroy,
        Fine = Scp914Rule.ToVanilla(ItemType.Coin),
        VeryFine = Scp914Rule.Weighted(
            (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardJanitor)),
            (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardScientist))),
    };

    public override ItemType BaseType => ItemType.Coin;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;
    public override string Name => "Quarter";
    public override string Description => "25セント硬貨。特に意味はない";
}
