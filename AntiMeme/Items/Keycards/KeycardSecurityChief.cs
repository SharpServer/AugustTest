using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 警備隊を指揮する警備長が持つキーカードです。
/// </summary>
public sealed class KeycardSecurityChief : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<KeycardArmoryLevel1>(),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.To<KeycardArmoryLevel2>(),
        VeryFine = Scp914Rule.Weighted(
            (0.85f, Scp914Rule.ToVanilla(ItemType.KeycardMTFCaptain)),
            (0.15f, Scp914Rule.To<MasterCard>())),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomMetalCase;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;

    /// <inheritdoc/>
    public override string Name => "警備長キーカード";

    /// <inheritdoc/>
    public override string Description => "警備隊を指揮したりする警備長が持つキーカード。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(1, 2, 2);

    /// <inheritdoc/>
    protected override string Label => "警備主任キーカード";

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(68, 68, 68, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(0, 0, 0, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Chf. Security";
}
