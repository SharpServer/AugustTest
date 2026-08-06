using AntiMeme.Items.Utility.Snav;
using AntiMeme.Items.Utility.Battery;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using UnityEngine;
using AntiMeme.Roles.ChaosInsurgency;
using AntiMeme.Roles.ClassD;
using AntiMeme.Roles.Guards;
using AntiMeme.Roles.Scientist;

namespace AntiMeme.Items.Scp914;

/// <summary>
/// バニラアイテムを SCP-914 に入れたときの変換表です。
///
/// カスタムアイテム側の規則は各クラスの <see cref="IScp914Upgradable.Scp914Rules"/> が持つので、
/// ここに出てくるのは「入力がバニラアイテムの場合」だけです。
/// 旧 <c>Scp914Changes.RegisterVanilla</c> 相当。
///
/// <para>
/// キーカードの序列 (この表はこれに沿っています):
/// 施設系 Janitor &lt; Scientist &lt; ZoneManager &lt; ResearchCoordinator &lt;
/// ContainmentEngineer &lt; FacilityManager &lt; SiteDirector &lt; O5。
/// 軍系 ArmoryLv1 &lt; Guard = ChaosIntruder &lt; ArmoryLv2 &lt; SecurityChief &lt;
/// MTFPrivate &lt; MTFOperative &lt; MTFCaptain = ChaosInsurgency &lt; SiteDirector &lt; O5。
/// </para>
/// </summary>
public static class VanillaScp914Rules
{
    /// <summary>
    /// どのアイテムでも引ける O5 のワイルドカード確率です。
    /// </summary>
    public const float O5WildcardChance = 0.002f;

    /// <summary>
    /// 床のアイテムだけが引ける、当たり枠のワイルドカード確率です。
    /// </summary>
    public const float PickupWildcardChance = 1f / 42f;

    /// <summary>
    /// 全アイテム共通の O5 ロールです。表の規則より先に判定します。
    /// </summary>
    public static Scp914Rule O5Wildcard => Scp914Rule.ToVanilla(ItemType.KeycardO5).WithChance(O5WildcardChance);

    /// <summary>
    /// 床のアイテムだけに乗る当たり枠です。インベントリには適用しません。
    /// </summary>
    public static Scp914Rule PickupWildcard => Scp914Rule
        .Weighted(
            (0.1f, Scp914Rule.To<Scp513Item>()),
            (0.9f, Scp914Rule.To<CapybaraMissile>()))
        .WithChance(PickupWildcardChance);

    /// <summary>
    /// バニラアイテムの変換表を作ります。<see cref="Scp914Handler.VanillaRules"/> の初期値です。
    /// </summary>
    public static Dictionary<ItemType, Scp914RuleSet> Build() => new Dictionary<ItemType, Scp914RuleSet>
    {
        // ===== 施設系キーカード =====
        [ItemType.KeycardJanitor] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Destroy,
            Coarse = Scp914Rule.To<Quarter>(2),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.Weighted(
                (0.7f, Scp914Rule.ToVanilla(ItemType.KeycardScientist)),
                (0.3f, Scp914Rule.To<KeycardArmoryLevel1>())),
            VeryFine = WithO5(
                0.005f,
                (0.045f, Scp914Rule.To<MasterCard>()),
                (0.95f, Scp914Rule.Destroy)),
        },

        [ItemType.KeycardScientist] = new Scp914RuleSet
        {
            Rough = Scp914Rule.To<Quarter>(2),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardJanitor),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.ToVanilla(ItemType.KeycardZoneManager),
            VeryFine = Scp914Rule.Weighted(
                (0.40f, Scp914Rule.ToVanilla(ItemType.KeycardZoneManager)),
                (0.35f, Scp914Rule.To<KeycardSiteNavigator>()),
                (0.20f, Scp914Rule.To<KeycardSupplyManager>()),
                (0.05f, Scp914Rule.To<MasterCard>())),
        },

        [ItemType.KeycardZoneManager] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Weighted(
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardJanitor)),
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardScientist))),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardScientist),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator),
            VeryFine = Scp914Rule.Weighted(
                (0.32f, Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator)),
                (0.32f, Scp914Rule.To<KeycardSurveillance>()),
                (0.10f, Scp914Rule.To<KeycardSupplyManager>()),
                (0.20f, Scp914Rule.To<KeycardChaosIntruder>()),
                (0.06f, Scp914Rule.To<MasterCard>())),
        },

        [ItemType.KeycardResearchCoordinator] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Weighted(
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardScientist)),
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardJanitor))),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardScientist),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.Weighted(
                (0.35f, Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator)),
                (0.25f, Scp914Rule.To<KeycardSiteNavigator>()),
                (0.25f, Scp914Rule.To<KeycardSupplyManager>()),
                (0.15f, Scp914Rule.To<KeycardSurveillance>())),
            VeryFine = WithO5(
                0.02f,
                (0.23f, Scp914Rule.ToVanilla(ItemType.KeycardContainmentEngineer)),
                (0.05f, Scp914Rule.To<MasterCard>()),
                (0.70f, Scp914Rule.Destroy)),
        },

        [ItemType.KeycardContainmentEngineer] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Weighted(
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardScientist)),
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator))),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.ToVanilla(ItemType.KeycardFacilityManager),
            VeryFine = WithO5(
                0.08f,
                (0.12f, Scp914Rule.To<MasterCard>()),
                (0.80f, Scp914Rule.Destroy)),
        },

        [ItemType.KeycardFacilityManager] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Weighted(
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardScientist)),
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator))),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardContainmentEngineer),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.Weighted(
                (0.55f, Scp914Rule.ToVanilla(ItemType.KeycardFacilityManager)),
                (0.25f, Scp914Rule.To<KeycardSiteDirector>()),
                (0.20f, Scp914Rule.To<MasterCard>())),
            VeryFine = WithO5(
                0.25f,
                (0.15f, Scp914Rule.To<MasterCard>()),
                (0.60f, Scp914Rule.Destroy)),
        },

        [ItemType.KeycardO5] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Weighted(
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardScientist)),
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardZoneManager))),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardFacilityManager),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.To<MasterCard>(),
            VeryFine = Scp914Rule.To<MasterCard>(),
        },

        // ===== 軍系キーカード =====
        [ItemType.KeycardGuard] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Destroy,
            Coarse = Scp914Rule.To<Quarter>(2),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.To<KeycardArmoryLevel2>(),
            VeryFine = Scp914Rule.Weighted(
                (0.49f, Scp914Rule.To<KeycardSecurityChief>()),
                (0.49f, Scp914Rule.To<KeycardChaosIntruder>()),
                (0.02f, Scp914Rule.To<MasterCard>())),
        },

        [ItemType.KeycardMTFPrivate] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Weighted(
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardScientist)),
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator))),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardResearchCoordinator),
            OneToOne = Scp914Rule.ToVanilla(ItemType.KeycardContainmentEngineer),
            Fine = Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative),
            VeryFine = Scp914Rule.Weighted(
                (0.475f, Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative)),
                (0.475f, Scp914Rule.To<KeycardArmoryLevel3>()),
                (0.05f, Scp914Rule.To<MasterCard>())),
        },

        [ItemType.KeycardMTFOperative] = new Scp914RuleSet
        {
            Rough = Scp914Rule.Weighted(
                (0.5f, Scp914Rule.ToVanilla(ItemType.KeycardGuard)),
                (0.5f, Scp914Rule.Destroy)),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardGuard),
            OneToOne = Scp914Rule.Keep,
            Fine = Scp914Rule.To<KeycardArmoryLevel3>(),
            VeryFine = Scp914Rule.Weighted(
                (0.46f, Scp914Rule.To<KeycardArmoryLevel3>()),
                (0.46f, Scp914Rule.ToVanilla(ItemType.KeycardMTFCaptain)),
                (0.08f, Scp914Rule.To<MasterCard>())),
        },

        [ItemType.KeycardMTFCaptain] = new Scp914RuleSet
        {
            Rough = Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative),
            Coarse = Scp914Rule.ToVanilla(ItemType.KeycardMTFOperative),
            OneToOne = Scp914Rule.ToVanilla(ItemType.KeycardChaosInsurgency),
            Fine = Scp914Rule.To<KeycardArmoryLevel3>(),
            VeryFine = Scp914Rule.Weighted(
                (0.35f, Scp914Rule.To<KeycardSiteDirector>()),
                (0.15f, Scp914Rule.To<MasterCard>()),
                (0.50f, Scp914Rule.Destroy)),
        },

        [ItemType.KeycardChaosInsurgency] = new Scp914RuleSet
        {
            Rough = Scp914Rule.To<KeycardConscripts>(),
            Coarse = Scp914Rule.To<KeycardChaosIntruder>(),
            OneToOne = Scp914Rule.ToVanilla(ItemType.KeycardMTFCaptain),
            Fine = Scp914Rule.To<KeycardArmoryLevel3>(),
            VeryFine = Scp914Rule.Weighted(
                (0.35f, Scp914Rule.To<KeycardSiteDirector>()),
                (0.15f, Scp914Rule.To<MasterCard>()),
                (0.50f, Scp914Rule.Destroy)),
        },

        // ===== キーカード以外 =====
        [ItemType.Adrenaline] = new Scp914RuleSet
        {
            VeryFine = Scp914Rule.To<SerumD>(),
        },

        [ItemType.SCP500] = new Scp914RuleSet
        {
            Fine = Scp914Rule.To<ClassXMemoryForcePill>(),
            VeryFine = Scp914Rule.To<ClassZMemoryForcePill>().WithChance(0.25f),
        },

        [ItemType.Radio] = new Scp914RuleSet
        {
            VeryFine = Scp914Rule.To<SNAV300>(),
        },

        [ItemType.MicroHID] = new Scp914RuleSet
        {
            Coarse = Scp914Rule.To<HIDTurret>().WithChance(0.5f),
        },

        [ItemType.GrenadeFlash] = new Scp914RuleSet
        {
            Fine = Scp914Rule.To<FlashBangE>().WithChance(1f / 3f),
        },

        [ItemType.SCP268] = new Scp914RuleSet
        {
            VeryFine = Scp914Rule.To<CloakGenerator>().WithChance(0.25f),
        },

        [ItemType.Coin] = new Scp914RuleSet
        {
            Coarse = Scp914Rule.To<Quarter>(),
        },

        [ItemType.GunRevolver] = new Scp914RuleSet
        {
            Fine = Scp914Rule.To<GunTacticalRevolver>().WithChance(0.5f),
        },

        [ItemType.SCP244a] = new Scp914RuleSet
        {
            Fine = Scp914Rule.To<ThrowableScp244>(),
            VeryFine = Scp914Rule.To<ThrowableScp244>(),
        },

        [ItemType.SCP244b] = new Scp914RuleSet
        {
            Fine = Scp914Rule.To<ThrowableScp244>(),
            VeryFine = Scp914Rule.To<ThrowableScp244>(),
        },

        [ItemType.SCP1344] = new Scp914RuleSet
        {
            Coarse = Scp914Rule.To<NvgBlue>(),
        },

        [ItemType.Flashlight] = new Scp914RuleSet
        {
            VeryFine = Scp914Rule.To<ScpcbBattery9V>().WithChance(0.5f),
        },
    };

    /// <summary>
    /// O5 の当たり枠を混ぜた重み付き抽選です。
    /// 残りの重みは <paramref name="others"/> 側の比率を保ったまま圧縮します。
    /// カスタムキーカード側の規則からも使います。
    /// </summary>
    public static Scp914Rule WithO5(float o5Chance, params (float Weight, Scp914Rule Rule)[] others)
    {
        float chance = Mathf.Clamp01(o5Chance);
        Scp914Rule toO5 = Scp914Rule.ToVanilla(ItemType.KeycardO5);

        if (chance >= 1f)
            return toO5;

        float otherTotal = 0f;

        foreach ((float weight, Scp914Rule _) in others)
            otherTotal += Mathf.Max(0f, weight);

        if (otherTotal <= 0f)
            return Scp914Rule.Weighted((chance, toO5));

        var entries = new (float Weight, Scp914Rule Rule)[others.Length + 1];
        entries[0] = (chance, toO5);

        for (int index = 0; index < others.Length; index++)
        {
            (float weight, Scp914Rule rule) = others[index];
            entries[index + 1] = (weight / otherTotal * (1f - chance), rule);
        }

        return Scp914Rule.Weighted(entries);
    }
}
