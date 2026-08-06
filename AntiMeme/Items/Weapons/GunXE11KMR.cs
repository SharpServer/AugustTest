using System;
using Footprinting;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;
using Utils;
using Sliced.API.Features.Attributes;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// XE-11K MR(通常ライフルモード)。投擲操作でグレネードランチャーへ切り替わります。
/// </summary>
[LegacyName("GunXE11KMR_Normal")]
public sealed class GunXE11KMR : HybridWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunE11SR;

    /// <inheritdoc/>
    public override string Name => "XE-11K MR";

    /// <inheritdoc/>
    public override string Description =>
        "<size=22>財団が開発した次世代兵装の試作型。\n" +
        "米軍のOICW計画や韓国のK-11複合小銃に触発され、\n" +
        "E-11をベースにライフルと弾倉式グレネードランチャーを合体させたマルチウェポンとなっている。\n" +
        "実際に試作がなされ実用に足る性能を発揮したものの、量産を前にして予算が尽きてしまい、\n" +
        "現在では数丁作られた試作品を一部の部隊が運用するに留まっている。</size>";

    /// <inheritdoc/>
    protected override string ModeName => "通常ライフル";

    /// <inheritdoc/>
    protected override Type NextMode => typeof(GunXE11KMRGrenadeLauncher);

    /// <inheritdoc/>
    protected override int MagazineSize => 40;

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments =>
    [
        AttachmentName.RifleBody,
        AttachmentName.SoundSuppressor,
        AttachmentName.AmmoCounter,
        AttachmentName.Laser,
        AttachmentName.NightVisionSight,
        AttachmentName.RecoilReducingStock,
        AttachmentName.StandardMagFMJ,
    ];

    /// <inheritdoc/>
    protected override bool AllowAttachmentChanges => false;
}

/// <summary>
/// XE-11K MR(グレネードランチャーモード)。着弾点で炸裂します。
/// </summary>
/// <remarks>
/// 旧実装は連射制限を <c>static Dictionary&lt;Player,int&gt;</c> と、
/// 1 秒ごとに全員分を減算し続けるコルーチンで持っていました。
/// アイテム 1 個 = 1 インスタンスなので、次に撃てる時刻を <see langword="float"/> 1 つで持てば済みます。
/// </remarks>
