using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;
using AntiMeme.Roles.Scientist;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 監視担当職員に支給されるキーカードです。
/// </summary>
public sealed class KeycardSurveillance : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.KeycardJanitor),
        OneToOne = Scp914Rule.ToVanilla(ItemType.KeycardGuard),
        Fine = Scp914Rule.To<KeycardArmoryLevel2>(),
        VeryFine = Scp914Rule.To<KeycardSecurityChief>(),
    };

    /// <inheritdoc/>
    public override string Name => "監視課職員キーカード";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.45f, 0.65f, 1f);

    /// <inheritdoc/>
    public override string Description => "監視担当職員に支給されるキーカード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(2, 1, 1);

    /// <inheritdoc/>
    protected override string Label => "SURVEILLANCE";

    /// <inheritdoc/>
    protected override Color32 LabelColor => new Color32(238, 246, 255, 255);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(55, 78, 116, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(12, 22, 40, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Surveillance";
}
