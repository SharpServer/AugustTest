using AntiMeme.Items;
using AntiMeme.Items.Scp914;
using InventorySystem.Items.Jailbird;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>使用回数で威力が上がるシュバルツシルト・クエィサァーです。</summary>
public sealed class SchwarzschildQuasar : MeleeWeapon, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.Jailbird),
        OneToOne = Scp914Rule.To<CaneOfTheStars>(),
        Fine = Scp914Rule.Keep,
        VeryFine = Scp914Rule.To<Weapons.SchwarzschildRailbreaker>(),
    };

    private static bool hooked;
    private int usedCount;
    private bool charged;

    public SchwarzschildQuasar() => Hook();

    public override ItemType BaseType => ItemType.Jailbird;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.blue;
    public override string Name => "シュバルツシルト・クエィサァー";
    public override string Description => "使用回数に応じて威力が上がる特殊なジェイルバード。チャージ攻撃はさらに強力。";

    protected override float MeleeDamage => 0f;

    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (!ev.IsAllowed || ev.Attacker?.CurrentItem is not { } item || Of(item.Serial) is not SchwarzschildQuasar quasar)
            return;

        float damage = quasar.usedCount switch
        {
            0 => 10f,
            <= 5 => 20f,
            <= 15 => 30f,
            <= 28 => 60f,
            _ => 777f,
        };

        if (quasar.charged)
            damage *= 2f;

        if (ev.DamageHandler is StandardDamageHandler handler)
            handler.Damage = damage;

        quasar.usedCount++;
        quasar.charged = false;
        quasar.ApplyWear();
    }

    private void ApplyWear()
    {
        JailbirdWearState wear = usedCount switch
        {
            <= 19 => JailbirdWearState.Healthy,
            <= 24 => JailbirdWearState.LowWear,
            <= 27 => JailbirdWearState.MediumWear,
            28 => JailbirdWearState.HighWear,
            29 => JailbirdWearState.AlmostBroken,
            _ => JailbirdWearState.Broken,
        };
        JailbirdDeteriorationTracker.ReceivedStates[Serial] = wear;
    }

    private static void OnProcessed(PlayerProcessedJailbirdMessageEventArgs ev)
    {
        if (Of(ev.JailbirdItem.Serial) is SchwarzschildQuasar quasar && ev.Message == JailbirdMessageType.ChargeStarted)
            quasar.charged = true;
    }

    private static void OnDying(PlayerDyingEventArgs ev)
    {
        if (ev.Attacker?.CurrentItem is { } item && Of(item.Serial) is SchwarzschildQuasar &&
            ev.DamageHandler is MicroHidDamageHandler)
        {
            ev.IsAllowed = false;
            ev.Player.Damage(100000f, "シュバルツシルト・クエィサァー");
        }
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.ProcessedJailbirdMessage += OnProcessed;
        PlayerEvents.Dying += OnDying;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.ProcessedJailbirdMessage -= OnProcessed;
            PlayerEvents.Dying -= OnDying;
            hooked = false;
        });
    }
}
