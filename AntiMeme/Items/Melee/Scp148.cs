using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>非人間対象へのダメージが 75% 増える精神遮断合金剣です。</summary>
public sealed class Scp148 : MeleeWeapon
{
    public override ItemType BaseType => ItemType.SCP1509;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;

    public override string Name => "SCP-148";

    public override string Description =>
        "プロメテウス研究所製の精神遮断合金剣。\nSCP特効ダメージ+75%、テレパシー完全防御。\n質量増加で効果増幅放出のリスク注意。";

    protected override bool AllowResurrect => false;

    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (!ev.Player.IsHuman && ev.DamageHandler is PlayerStatsSystem.StandardDamageHandler damage)
            damage.Damage *= 1.75f;
    }
}
