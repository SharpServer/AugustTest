using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// サイト-02 の供給管理課の職員が持つキーカードです。
/// </summary>
public sealed class KeycardSupplyManager : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.To<Utility.Quarter>(2),
        Coarse = Scp914Rule.ToVanilla(ItemType.KeycardScientist),
        OneToOne = Scp914Rule.To<KeycardSiteNavigator>(),
        Fine = Scp914Rule.Weighted(
            (0.55f, Scp914Rule.To<KeycardSurveillance>()),
            (0.40f, Scp914Rule.To<KeycardArmoryLevel2>()),
            (0.05f, Scp914Rule.To<MasterCard>())),
        VeryFine = Scp914Rule.Weighted(
            (0.60f, Scp914Rule.To<KeycardSecurityChief>()),
            (0.30f, Scp914Rule.ToVanilla(ItemType.KeycardFacilityManager)),
            (0.10f, Scp914Rule.To<MasterCard>())),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomManagement;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 107, 137, 255);

    /// <inheritdoc/>
    public override string Name => "供給管理官キーカード";

    /// <inheritdoc/>
    public override string Description => "サイト-02の供給管理課の職員が持つキーカード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(0, 0, 2);

    /// <inheritdoc/>
    protected override string Label => "SUPPLY MANAGER";

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(54, 120, 140, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(0, 107, 137, 255);
}
