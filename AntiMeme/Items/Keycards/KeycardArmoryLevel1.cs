using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 軽武装ロッカーを開けられる武器アクセスカードです。
/// </summary>
public sealed class KeycardArmoryLevel1 : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.ToVanilla(ItemType.KeycardGuard),
        Fine = Scp914Rule.To<KeycardChaosIntruder>(),
        VeryFine = Scp914Rule.Weighted(
            (0.95f, Scp914Rule.To<KeycardConscripts>()),
            (0.05f, Scp914Rule.To<MasterCard>())),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomTaskForce;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.35f, 0.55f, 0.8f);

    /// <inheritdoc/>
    public override string Name => "Armory Level 1 Keycard";

    /// <inheritdoc/>
    public override string Description => "軽武装ロッカーを開けられる武器アクセスカード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(0, 1, 1);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(58, 84, 104, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(10, 18, 26, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Armory I";

    /// <inheritdoc/>
    protected override string SerialNumber => "A-1";

    /// <inheritdoc/>
    protected override int Rank => 3;
}
