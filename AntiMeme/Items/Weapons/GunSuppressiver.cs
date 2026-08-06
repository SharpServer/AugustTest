using AntiMeme.Items.Bases;
using InventorySystem.Items.Firearms.Attachments;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// Suppressiver。カオスの工作員向けサブマシンガンです。
///
/// <para>
/// 旧実装は発砲音を専用クリップへ差し替えていました。<b>音は保留</b>です。
/// 音声層 (担当 15) の再構築後にその上へ載せます。
/// </para>
/// </summary>
public sealed class GunSuppressiver : CustomWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunFSP9;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 75, 0, 255);

    /// <inheritdoc/>
    public override string Name => "Suppressiver";

    /// <inheritdoc/>
    public override string Description =>
        "カオスが開発した工作員向けの特殊なサブマシンガン。\n" +
        "射撃時に妨害電波とノイズを発し、射撃音の補足を妨害する。\n" +
        "消音性能には優れているものの、安定性はイマイチ。";

    /// <inheritdoc/>
    protected override float Damage => 30f;

    /// <inheritdoc/>
    protected override int MagazineSize => 42;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1.16f, 1f, 0.96f);

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments =>
    [
        AttachmentName.DotSight,
        AttachmentName.SoundSuppressor,
        AttachmentName.AmmoCounter,
        AttachmentName.RetractedStock,
    ];

    /// <inheritdoc/>
    protected override bool AllowAttachmentChanges => false;
}
