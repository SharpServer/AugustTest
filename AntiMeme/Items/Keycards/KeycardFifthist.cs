using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 第五教会が施設に侵入するために作ったアクセスデバイスです。
/// </summary>
public sealed class KeycardFifthist : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<Scp1425>(),
        OneToOne = Scp914Rule.Destroy,
        Fine = Scp914Rule.To<KeycardFifthistPriest>(),
        VeryFine = Scp914Rule.To<Mindblaster>().WithChance(1f / 3f),
    };

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomTaskForce;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.magenta;

    /// <inheritdoc/>
    public override string Name => "第五教会 第五デバイス";

    /// <inheritdoc/>
    public override string Description =>
        "第五教会が目的を達成するために作られたアクセスデバイス。\n扉やゲートを第五することで施設に侵入する。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(3, 3, 2);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(255, 0, 250, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(255, 255, 255, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Rsc. Fifth";

    /// <inheritdoc/>
    protected override string SerialNumber => "555555555555";

    /// <inheritdoc/>
    protected override int Rank => 1;
}
