using AntiMeme.Items;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using InventorySystem.Items.MicroHID.Modules;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>低出力・連続照射型 MicroHID です。エネルギーを常時満タンに保ちます。</summary>
public sealed class HIDTurret : CustomItem, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.ToVanilla(ItemType.MicroHID),
        VeryFine = Scp914Rule.To<GunGoCTurret>(),
    };

    private static bool hooked;

    public HIDTurret() => Hook();

    public override ItemType BaseType => ItemType.MicroHID;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;
    public override string Name => "MicroHID-C（連続運用モデル）";
    public override string Description => "小出力の連続照射型 MicroHID。エネルギー無制限で、強チャージも低出力に抑える。";

    protected override void OnEquipped(Player player)
    {
        PlayerScope.Of(player).RunLoop(.25f, owner =>
        {
            if (owner.CurrentItem is not MicroHIDItem item || Of(item.Serial) is not HIDTurret)
                return;

            item.Energy = 1f;
        });
    }

    private static void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker?.CurrentItem is not { } item || Of(item.Serial) is not HIDTurret)
            return;

        if (ev.DamageHandler is MicroHidDamageHandler damage)
            damage.Damage = 20f;
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.Hurting += OnHurting;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.Hurting -= OnHurting;
            hooked = false;
        });
    }
}
