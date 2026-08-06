using AntiMeme.Items.Bases;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// SL-8G。カオス側のスナイパーライフルです。
/// </summary>
public sealed class GunSL8 : CustomWeapon
{
    private const float HeadshotBonus = 15f;
    private const float VelocityMultiplier = 1.025f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunFRMG0;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 75, 0, 255);

    /// <inheritdoc/>
    public override string Name => "SL-8G";

    /// <inheritdoc/>
    public override string Description => "カオスが所有するスナイパーライフル。質はそこそこ";

    /// <inheritdoc/>
    protected override float Damage => 40f;

    /// <inheritdoc/>
    protected override int MagazineSize => 5;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.75f);

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments =>
    [
        AttachmentName.ScopeSight,
        AttachmentName.SoundSuppressor,
    ];

    /// <inheritdoc/>
    protected override bool AllowAttachmentChanges => false;

    /// <inheritdoc/>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler is not StandardDamageHandler handler) return;

        handler.StartVelocity *= VelocityMultiplier;

        if (handler.Hitbox == HitboxType.Headshot)
            handler.Damage += HeadshotBonus;
    }
}
