using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 最高位の武器アクセスを持つ戦術カードです。
/// </summary>
public sealed class KeycardArmoryLevel3 : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.To<KeycardArmoryLevel2>(),
        Coarse = Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative),
        OneToOne = Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative),
        Fine = Scp914Rule.Weighted(
            (0.35f, Scp914Rule.Keep),
            (0.35f, Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative)),
            (0.20f, Scp914Rule.To<KeycardSecurityChief>()),
            (0.10f, Scp914Rule.To<MasterCard>())),
        VeryFine = Scp914Rule.Weighted(
            (0.45f, Scp914Rule.ToVanilla(ItemType.KeycardMTFCaptain)),
            (0.45f, Scp914Rule.ToVanilla(ItemType.KeycardChaosInsurgency)),
            (0.10f, Scp914Rule.To<MasterCard>())),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomTaskForce;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.1f, 0.25f, 1f);

    /// <inheritdoc/>
    public override string Name => "Armory Level 3 Keycard";

    /// <inheritdoc/>
    public override string Description => "最高位の武器アクセスを持つ戦術カード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(0, 3, 2);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(30, 51, 112, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(4, 9, 24, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Armory III";

    /// <inheritdoc/>
    protected override string SerialNumber => "A-3";

    /// <inheritdoc/>
    protected override int Rank => 1;
}
