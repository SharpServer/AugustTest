using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// S-NAV 担当職員に支給されるキーカードです。
/// </summary>
public sealed class KeycardSiteNavigator : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.KeycardScientist),
        OneToOne = Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator),
        Fine = Scp914Rule.To<KeycardChaosIntruder>(),
        VeryFine = Scp914Rule.Weighted(
            (2f / 3f, Scp914Rule.To<KeycardArmoryLevel2>()),
            (1f / 3f, Scp914Rule.To<MasterCard>())),
    };

    /// <inheritdoc/>
    public override string Name => "施設ナビゲーターキーカード";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.35f, 0.95f, 0.85f);

    /// <inheritdoc/>
    public override string Description => "S-NAV担当職員に支給されるキーカード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(1, 0, 2);

    /// <inheritdoc/>
    protected override string Label => "SITE NAVIGATOR";

    /// <inheritdoc/>
    protected override Color32 LabelColor => new Color32(20, 36, 48, 255);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(96, 190, 176, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(9, 42, 46, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Site Navigator";
}
