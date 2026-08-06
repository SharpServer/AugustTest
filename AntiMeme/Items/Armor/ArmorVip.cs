using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using InventorySystem.Items.Armor;
using UnityEngine;

namespace AntiMeme.Items.Armor;

/// <summary>
/// 要人用アーマー。防護に超特化していて、弾道防護は上限いっぱいです。
/// </summary>
public sealed class ArmorVip : CustomArmor, IScp914Upgradable
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.ArmorHeavy;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(185, 75, 255, 255);

    /// <inheritdoc/>
    public override string Name => "要人用アーマー";

    /// <inheritdoc/>
    public override string Description => "要人の命を守るために、防護に超特化したアーマー。";

    /// <inheritdoc/>
    protected override int VestEfficacy => 100;

    /// <inheritdoc/>
    protected override int HelmetEfficacy => 100;

    /// <summary>
    /// 重装甲のわりにスタミナを食いません。バニラの重装アーマーは 1.5。
    /// </summary>
    protected override float StaminaUseMultiplier => 0.2f;

    /// <inheritdoc/>
    protected override BodyArmor.ArmorAmmoLimit[] AmmoLimits =>
    [
        AmmoLimit(ItemType.Ammo9x19, 400),
        AmmoLimit(ItemType.Ammo556x45, 400),
        AmmoLimit(ItemType.Ammo12gauge, 100),
        AmmoLimit(ItemType.Ammo44cal, 50),
    ];

    /// <inheritdoc/>
    protected override BodyArmor.ArmorCategoryLimitModifier[] CategoryLimits =>
    [
        CategoryLimit(ItemCategory.Firearm, 3),
        CategoryLimit(ItemCategory.Grenade, 3),
    ];

    /// <summary>
    /// これ以上は上がりません。粗い設定では壊れ、雑な設定で歩兵用へ落ちます。
    /// </summary>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<ArmorInfantry>(),
    };
}
