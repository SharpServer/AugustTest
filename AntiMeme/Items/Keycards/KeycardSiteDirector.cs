using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 施設管理長用の管理カードです。ほぼすべてにアクセスできます。
/// </summary>
public sealed class KeycardSiteDirector : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.ToVanilla(ItemType.KeycardFacilityManager),
        Coarse = Scp914Rule.ToVanilla(ItemType.KeycardContainmentEngineer),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.Destroy,
        VeryFine = VanillaScp914Rules.WithO5(0.50f, (0.50f, Scp914Rule.Destroy)),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomManagement;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(1f, 0.25f, 0.35f);

    /// <inheritdoc/>
    public override string Name => "施設管理長キーカード";

    /// <inheritdoc/>
    public override string Description => "施設管理長用の管理カード。ほぼ全てにアクセスできる。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(3, 3, 3);

    /// <inheritdoc/>
    protected override string Label => "SITE DIRECTOR";

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(156, 41, 62, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(35, 8, 14, 255);
}
