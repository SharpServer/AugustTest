using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 重武装ロッカーと EPS を扱える武器アクセスカードです。
/// </summary>
public sealed class KeycardArmoryLevel2 : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.ToVanilla(ItemType.KeycardGuard),
        Coarse = Scp914Rule.To<KeycardArmoryLevel1>(),
        OneToOne = Scp914Rule.To<KeycardConscripts>(),
        Fine = Scp914Rule.To<KeycardArmoryLevel3>(),
        VeryFine = Scp914Rule.Weighted(
            (0.85f, Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative)),
            (0.15f, Scp914Rule.To<MasterCard>())),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomTaskForce;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.25f, 0.45f, 0.95f);

    /// <inheritdoc/>
    public override string Name => "Armory Level 2 Keycard";

    /// <inheritdoc/>
    public override string Description => "重武装ロッカーと EPS を扱える武器アクセスカード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(0, 2, 1);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(44, 79, 132, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(7, 16, 31, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Armory II";

    /// <inheritdoc/>
    protected override string SerialNumber => "A-2";

    /// <inheritdoc/>
    protected override int Rank => 2;
}
