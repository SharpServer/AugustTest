using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using InventorySystem.Items.Armor;
using UnityEngine;

namespace AntiMeme.Items.Armor;

/// <summary>
/// 歩兵用アーマー。大規模部隊の標準装備で、弾薬を多く持ち歩けます。
/// </summary>
public sealed class ArmorInfantry : CustomArmor, IScp914Upgradable
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.ArmorCombat;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;

    /// <inheritdoc/>
    public override string Name => "歩兵用アーマー";

    /// <inheritdoc/>
    public override string Description => "大規模な部隊の歩兵に使われる戦闘アーマー。";

    /// <inheritdoc/>
    protected override int VestEfficacy => 80;

    /// <inheritdoc/>
    protected override int HelmetEfficacy => 85;

    /// <summary>
    /// 軽量な作りでスタミナをほとんど食いません。バニラの戦闘用アーマーは 1.15。
    /// </summary>
    protected override float StaminaUseMultiplier => 0.15f;

    /// <inheritdoc/>
    protected override BodyArmor.ArmorAmmoLimit[] AmmoLimits =>
    [
        AmmoLimit(ItemType.Ammo9x19, 220),
        AmmoLimit(ItemType.Ammo556x45, 200),
        AmmoLimit(ItemType.Ammo762x39, 130),
        AmmoLimit(ItemType.Ammo12gauge, 80),
    ];

    /// <inheritdoc/>
    protected override BodyArmor.ArmorCategoryLimitModifier[] CategoryLimits =>
    [
        CategoryLimit(ItemCategory.Firearm, 3),
        CategoryLimit(ItemCategory.Grenade, 3),
    ];

    /// <summary>
    /// 粗い設定で普通の軽装アーマーに落ち、細かい設定で要人用へ上がります。
    /// </summary>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.ToVanilla(ItemType.ArmorLight),
        Coarse = Scp914Rule.ToVanilla(ItemType.ArmorLight),
        Fine = Scp914Rule.To<ArmorVip>(),
        VeryFine = Scp914Rule.To<ArmorVip>(),
    };
}
