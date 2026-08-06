using InventorySystem.Items.Armor;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Bases;

/// <summary>
/// カスタムアーマーの防護計算方式です。
/// </summary>
public enum ArmorProtection
{
    /// <summary>
    /// ゲーム標準の弾道防護だけを使います。実軽減率は「効力値 × (1 - 弾の貫通率)」です。
    /// </summary>
    Ballistic,

    /// <summary>
    /// 貫通率に左右されない固定割合の軽減だけを使います。標準の効力値は 0 にします。
    /// </summary>
    Fixed,

    /// <summary>
    /// 標準の弾道防護に加えて固定割合の軽減も掛けます。強くなりすぎやすいので意図して選ぶときだけ。
    /// </summary>
    Both,
}

/// <summary>
/// カスタムアーマーの基底です。
///
/// <para>
/// アーマーの取得だけは <see cref="PlayerEvents.PickedUpItem"/> ではなく
/// <see cref="PlayerEvents.PickedUpArmor"/> を通ります。シリアルは引き継がれるので追跡は切れませんが、
/// <see cref="CustomItem.Customize(Item)"/> が呼ばれないため、ここで焼き直しています。
/// </para>
/// </summary>
public abstract class CustomArmor : CustomItem
{
    private static bool hooked;

    protected CustomArmor() => Hook();

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.ArmorCombat;

    /// <summary>
    /// このアーマーの実体です。地面に落ちていれば null。
    /// </summary>
    public BodyArmorItem Armor => Item as BodyArmorItem;

    /// <summary>
    /// 防護計算方式です。
    /// </summary>
    protected virtual ArmorProtection Protection => ArmorProtection.Ballistic;

    /// <summary>
    /// 胴体のゲーム標準弾道防護値 (0-100) です。
    /// </summary>
    protected virtual int VestEfficacy => 80;

    /// <summary>
    /// 頭部のゲーム標準弾道防護値 (0-100) です。
    /// </summary>
    protected virtual int HelmetEfficacy => 80;

    /// <summary>
    /// 胴体への固定軽減率 (0-100%) です。
    /// </summary>
    protected virtual float VestReduction => 0f;

    /// <summary>
    /// 頭部への固定軽減率 (0-100%) です。
    /// </summary>
    protected virtual float HelmetReduction => 0f;

    /// <summary>
    /// 手足への固定軽減率 (0-100%) です。
    /// </summary>
    protected virtual float LimbReduction => VestReduction;

    /// <summary>
    /// 銃弾以外 (爆発・落下・SCP の攻撃など) への固定軽減率 (0-100%) です。
    /// </summary>
    protected virtual float GeneralReduction => 0f;

    /// <summary>
    /// スタミナ消耗倍率です。
    /// </summary>
    protected virtual float StaminaUseMultiplier => 1.15f;

    /// <summary>
    /// 移動速度倍率です。負値ならベースアーマーの値のまま。
    /// </summary>
    protected virtual float MovementSpeedMultiplier => -1f;

    /// <summary>
    /// 重量です。負値ならベースアーマーの値のまま。
    /// </summary>
    protected virtual float Weight => -1f;

    /// <summary>
    /// Class-D / 研究員に掛かるデメリット倍率です。負値ならベースアーマーの値のまま。
    /// </summary>
    protected virtual float CivilianDownsidesMultiplier => -1f;

    /// <summary>
    /// 弾薬所持上限です。空なら変更しません。
    /// </summary>
    protected virtual BodyArmor.ArmorAmmoLimit[] AmmoLimits => [];

    /// <summary>
    /// カテゴリ所持上限です。空なら変更しません。
    /// </summary>
    protected virtual BodyArmor.ArmorCategoryLimitModifier[] CategoryLimits => [];

    /// <summary>読みやすい弾薬上限の書き方です。</summary>
    protected static BodyArmor.ArmorAmmoLimit AmmoLimit(ItemType ammo, int limit) =>
        new BodyArmor.ArmorAmmoLimit { AmmoType = ammo, Limit = (ushort)limit };

    /// <summary>読みやすいカテゴリ上限の書き方です。</summary>
    protected static BodyArmor.ArmorCategoryLimitModifier CategoryLimit(ItemCategory category, int limit) =>
        new BodyArmor.ArmorCategoryLimitModifier { Category = category, Limit = (byte)limit };

    /// <summary>
    /// この攻撃に固定軽減を掛けるか。攻撃者や武器で分けたいときに override します。
    /// </summary>
    protected virtual bool CanReduce(PlayerHurtingEventArgs ev) => true;

    /// <summary>
    /// この攻撃に掛ける固定軽減率 (0-100%) です。
    /// </summary>
    protected virtual float GetReduction(PlayerHurtingEventArgs ev, StandardDamageHandler damage)
    {
        if (damage is not FirearmDamageHandler)
            return GeneralReduction;

        return damage.Hitbox switch
        {
            HitboxType.Headshot => HelmetReduction,
            HitboxType.Limb => LimbReduction,
            _ => VestReduction,
        };
    }

    /// <inheritdoc/>
    protected override void Customize(Item item)
    {
        if (item is not BodyArmorItem wrapper) return;

        BodyArmor armor = wrapper.Base;
        bool ballistic = Protection != ArmorProtection.Fixed;

        armor.VestEfficacy = ballistic ? Mathf.Clamp(VestEfficacy, 0, 100) : 0;
        armor.HelmetEfficacy = ballistic ? Mathf.Clamp(HelmetEfficacy, 0, 100) : 0;
        armor._staminaUseMultiplier = StaminaUseMultiplier;

        if (MovementSpeedMultiplier >= 0f)
            armor._movementSpeedMultiplier = MovementSpeedMultiplier;

        if (Weight >= 0f)
            armor._weight = Weight;

        if (CivilianDownsidesMultiplier >= 0f)
            armor.CivilianClassDownsidesMultiplier = CivilianDownsidesMultiplier;

        if (AmmoLimits.Length > 0)
            armor.AmmoLimits = AmmoLimits;

        if (CategoryLimits.Length > 0)
            armor.CategoryLimits = CategoryLimits;

        // 所持上限を変えたので、ゲーム側に持ち物の再評価をさせる。
        if (armor.Owner is { } owner)
            BodyArmorUtils.SetPlayerDirty(owner);
    }

    private void Reduce(PlayerHurtingEventArgs ev, StandardDamageHandler damage)
    {
        if (Protection == ArmorProtection.Ballistic || !CanReduce(ev)) return;

        float reduction = Mathf.Clamp(GetReduction(ev, damage), 0f, 100f) / 100f;
        damage.Damage = Mathf.Max(0f, damage.Damage * (1f - reduction));
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;

        // 取得時の Customize は Sliced の CustomItem が PickedUpArmor を購読して面倒を見る。
        // ここで購読するのはアーマー固有のダメージ軽減だけ。
        PlayerEvents.Hurting += OnAnyHurting;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.Hurting -= OnAnyHurting;
            hooked = false;
        });
    }

    private static void OnAnyHurting(PlayerHurtingEventArgs ev)
    {
        if (!ev.IsAllowed) return;
        if (ev.DamageHandler is not StandardDamageHandler damage || damage.Damage <= 0f) return;
        if (!ev.Player.Inventory.TryGetBodyArmor(out BodyArmor worn)) return;

        if (Of(worn.ItemSerial) is CustomArmor armor)
            armor.Reduce(ev, damage);
    }
}
