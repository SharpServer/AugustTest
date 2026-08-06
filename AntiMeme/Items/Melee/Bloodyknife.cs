using CustomPlayerEffects;
using InventorySystem.Items.Scp1509;
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>
/// Bloodyknife。刺した相手を出血させます。SCP-1509 本来の蘇生能力は失われています。
/// </summary>
public sealed class Bloodyknife : MeleeWeapon
{
    private const float BleedDuration = 15f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SCP1509;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(220, 20, 60, 255);

    /// <inheritdoc/>
    public override string Name => "Bloodyknife";

    /// <inheritdoc/>
    public override string Description =>
        "Class-D Bloodfiendが脱走時に殺害した警備員から失敬したナイフ。\n" +
        "黒かった柄の部分は赤く染まり、刃には鮮血がこびり付いている。";

    /// <inheritdoc/>
    protected override float MeleeDamage => 20f;

    /// <inheritdoc/>
    protected override bool AllowResurrect => false;

    /// <summary>
    /// 刺し傷は塞がりません。
    /// </summary>
    /// <remarks>
    /// 旧実装は「このナイフを持っているあいだの全ダメージ」で出血させていたので、
    /// 銃や爆発で与えたダメージでも出血が付いていました。ここではナイフ自身の一撃だけに絞っています。
    /// </remarks>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler is not Scp1509DamageHandler) return;

        ev.Player.EnableEffect<Bleeding>(1, BleedDuration);
    }
}
