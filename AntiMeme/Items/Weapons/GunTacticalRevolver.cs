using AntiMeme.Items.Bases;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// Tactical Revolver。ヘッドショットで脳震盪を与え、リロード直後は狙いが安定するリボルバーです。
/// </summary>
public sealed class GunTacticalRevolver : CustomWeapon
{
    private const byte ConcussionIntensity = 255;
    private const float ConcussionDuration = 10f;
    private const byte FocusIntensity = 2;
    private const float FocusDuration = 12f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunRevolver;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;

    /// <inheritdoc/>
    public override string Name => "Tactical Revolver";

    /// <inheritdoc/>
    public override string Description =>
        "ヘッドショットをすると暫く脳震盪を与えられる精密なリボルバー。\nリロード時暫くは精度良く扱える";

    /// <inheritdoc/>
    protected override float Damage => 30f;

    /// <inheritdoc/>
    protected override int MagazineSize => 7;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.15f);

    /// <inheritdoc/>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler is StandardDamageHandler { Hitbox: HitboxType.Headshot })
            ev.Player.EnableEffect<Concussed>(ConcussionIntensity, ConcussionDuration);
    }

    /// <inheritdoc/>
    protected override void OnReloaded(PlayerReloadedWeaponEventArgs ev) =>
        ev.Player.EnableEffect<Scp1853>(FocusIntensity, FocusDuration);
}
