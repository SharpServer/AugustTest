using System.Linq;
using AntiMeme.Items;
using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Nvg;

/// <summary>
/// 反ミームゴーグル。所持している攻撃者が第五教会員へ与えるダメージを 10% 増やします。
/// </summary>
public sealed class AntiMemeGoggle : CustomNvg
{
    private static bool hooked;

    public AntiMemeGoggle() => Hook();

    public override string Name => "反ミームゴーグル";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;

    public override string Description =>
        "反ミーム的影響を遮断する財団の最新兵器。\n<color=red>SCP-3005への攻撃が通じるようになる</color>";

    private static void Hook()
    {
        if (hooked)
            return;

        hooked = true;
        PlayerEvents.Hurting += OnHurting;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.Hurting -= OnHurting;
            hooked = false;
        });
    }

    private static void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is not { } attacker ||
            !attacker.Items.Any(item => CustomItem.Of(item.Serial) is AntiMemeGoggle) ||
            !CustomTeam.Get<FifthistTeam>().Includes(ev.Player))
            return;

        if (ev.DamageHandler is StandardDamageHandler damage)
            damage.Damage *= 1.1f;
    }
}
