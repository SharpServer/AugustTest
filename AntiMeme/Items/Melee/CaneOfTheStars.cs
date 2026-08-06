using AntiMeme.Items.Scp914;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Sliced.API.Features;
using AntiMeme.Roles.Fifthist;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>
/// 第五教会の案内人が持つ杖。溜め攻撃は当たればまず助からない威力です。
/// </summary>
public sealed class CaneOfTheStars : MeleeWeapon, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<Keycards.KeycardFifthistPriest>(),
        OneToOne = Scp914Rule.ToVanilla(ItemType.SCP1509),
        Fine = Scp914Rule.Keep,
        VeryFine = Scp914Rule.Weighted(
            (1f / 77f, Scp914Rule.To<SchwarzschildQuasar>()),
            (76f / 77f, Scp914Rule.Destroy)),
    };

    private static bool hooked;

    public CaneOfTheStars() => HookConversion();

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Jailbird;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.magenta;

    /// <inheritdoc/>
    public override string Name => "Cane of the Stars";

    /// <inheritdoc/>
    public override string Description =>
        "第五教会の案内人が持つ杖。\n" +
        "殴った対象の脳内に第五主義思想を直接流し込み、\n" +
        "強制的に第五主義者に改宗させる能力を持つ";

    /// <inheritdoc/>
    protected override float MeleeDamage => 55f;

    /// <inheritdoc/>
    protected override float ChargeDamage => 555f;

    private static void HookConversion()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.Dying += OnDying;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.Dying -= OnDying;
            hooked = false;
        });
    }

    private static void OnDying(PlayerDyingEventArgs ev)
    {
        if (ev.Player is not { IsDestroyed: false } victim ||
            ev.Attacker is not { IsDestroyed: false } attacker ||
            attacker == victim ||
            attacker.CurrentItem is not { } held ||
            CustomItem.Of(held.Serial) is not CaneOfTheStars)
        {
            return;
        }

        // 旧仕様は「杖を構えた攻撃者による致死ダメージ」を改宗に置き換えるものです。
        // CustomRole.Spawn が役職・体力・装備を一括適用するため、瀕死状態も残りません。
        ev.IsAllowed = false;
        CustomRole.Spawn<FifthistConvert>(victim);
    }
}
