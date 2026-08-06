using System;
using Footprinting;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;
using Utils;
using Sliced.API.Features.Attributes;

namespace AntiMeme.Items.Weapons;

[LegacyName("GunXE11KMR_GL")]
public sealed class GunXE11KMRGrenadeLauncher : HybridWeapon
{
    private const float ShotCooldown = 5f;

    private float readyAt;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunFRMG0;

    /// <inheritdoc/>
    public override string Name => "XE-11K MR";

    /// <inheritdoc/>
    public override string Description => "弾倉式グレネードランチャー。着弾点で炸裂する。";

    /// <inheritdoc/>
    protected override string ModeName => "グレネードランチャー";

    /// <inheritdoc/>
    protected override Type NextMode => typeof(GunXE11KMR);

    /// <inheritdoc/>
    protected override int MagazineSize => 4;

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments =>
    [
        AttachmentName.MuzzleBrake,
        AttachmentName.Laser,
        AttachmentName.NightVisionSight,
        AttachmentName.HeavyStock,
        AttachmentName.DrumMagAP,
    ];

    /// <inheritdoc/>
    protected override bool AllowAttachmentChanges => false;

    /// <inheritdoc/>
    protected override void OnShooting(PlayerShootingWeaponEventArgs ev)
    {
        base.OnShooting(ev);

        if (Time.time < readyAt)
            ev.IsAllowed = false;
    }

    /// <inheritdoc/>
    protected override void OnShot()
    {
        base.OnShot();

        readyAt = Time.time + ShotCooldown;
    }

    /// <inheritdoc/>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is not { } attacker) return;

        Vector3 position = ev.Player.Position;

        ExplosionUtils.ServerExplode(position, new Footprint(attacker.ReferenceHub), ExplosionType.Grenade);

        if (!ev.Player.IsAlive)
            attacker.SendHitMarker(1.5f);
    }
}
