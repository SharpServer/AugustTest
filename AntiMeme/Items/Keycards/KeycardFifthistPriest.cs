using AntiMeme.Items.Bases;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Scp914;
using AntiMeme.Items.Weapons;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 第五教会の司祭が持つアクセスデバイスです。核へのアクセスも持ちます。
/// </summary>
public sealed class KeycardFifthistPriest : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<KeycardFifthist>(),
        OneToOne = Scp914Rule.Destroy,
        Fine = Scp914Rule.To<Mindblaster>(),
        VeryFine = Scp914Rule.To<CaneOfTheStars>(),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomTaskForce;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.magenta;

    /// <inheritdoc/>
    public override string Name => "第五教会 司祭デバイス";

    /// <inheritdoc/>
    public override string Description =>
        "第五教会が目的を達成するために作られた司祭用のアクセスデバイス。\n扉やゲートを第五することで施設に侵入する。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(3, 3, 3);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(255, 0, 255, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(255, 255, 255, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Pst. Fifth";

    /// <inheritdoc/>
    protected override string SerialNumber => "555555555555";

    /// <inheritdoc/>
    protected override int Rank => 1;
}
