using AntiMeme.Items.Bases;
using AntiMeme.Roles.Scps;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// MTF-E11-AR。E-11 が持つ対物ライフルです。1 射で弾倉を撃ち尽くします。
///
/// <para>
/// 旧実装は発砲音を爆発音へ差し替えていました (<c>Player.SendingGunSound</c>)。
/// LabAPI に同等のイベントが無いため<b>音は保留</b>です。音声層 (担当 15) の上に載せます。
/// </para>
/// </summary>
public sealed class GunM82 : CustomWeapon
{
    private const float HeadshotBonus = 30f;
    private const float VelocityMultiplier = 4f;
    private const float Scp173Multiplier = 2.5f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunE11SR;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;

    /// <inheritdoc/>
    public override string Name => "MTF-E11-AR";

    /// <inheritdoc/>
    public override string Description => "E11が所持する対物ライフル";

    /// <inheritdoc/>
    protected override float Damage => 80f;

    /// <inheritdoc/>
    protected override int MagazineSize => 30;

    /// <inheritdoc/>
    protected override int AmmoDrain => 30;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 2.25f);

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments =>
    [
        AttachmentName.ScopeSight,
        AttachmentName.LowcapMagAP,
        AttachmentName.RifleBody,
        AttachmentName.RecoilReducingStock,
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

        if (ev.Player.Role == RoleTypeId.Scp173 || CustomRole.Is<Scp173>(ev.Player))
            handler.Damage *= Scp173Multiplier;
    }
}
