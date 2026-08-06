using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 新入りや潜入工作員が持つ、アクセスデバイスの代わりとなるカードです。
/// </summary>
public sealed class KeycardConscripts : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<KeycardChaosIntruder>(),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.ToVanilla(ItemType.KeycardChaosInsurgency),
        VeryFine = VanillaScp914Rules.WithO5(
            0.02f,
            (0.08f, Scp914Rule.To<MasterCard>()),
            (0.90f, Scp914Rule.Destroy)),
    };

    /// <inheritdoc/>
    public override string Name => "カオス 破壊カード";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;

    /// <inheritdoc/>
    public override string Description =>
        "新入りや潜入工作員が持つ、アクセスデバイスの代わりとなるカード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(2, 2, 2);

    /// <inheritdoc/>
    protected override string Label => "BREAKING CARD";

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(0, 68, 0, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(0, 0, 0, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Chaos Conscript";
}
