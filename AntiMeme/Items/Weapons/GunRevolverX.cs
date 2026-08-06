using AntiMeme.Items.Bases;
using CameraShaking;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// Revolver-X。ある博士の特注リボルバーです。
///
/// <para>
/// 旧実装はピックアップの見た目を ProjectMER スキマティック
/// (<c>Alienisolation_Revolver</c>) に差し替えていました。
/// <b>見た目の差し替えは保留</b>です。マップ層の具象オブジェクト (担当 13b) と
/// 同じ仕組みに載せます。
/// </para>
/// </summary>
public sealed class GunRevolverX : CustomWeapon
{
    private static readonly RecoilSettings Recoil = new RecoilSettings(0.01f, 1856f, 2000f, 1507f, 296.5f);

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunRevolver;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(95, 214, 71, 255);

    /// <inheritdoc/>
    protected override string PickupModel => "Alienisolation_Revolver";

    /// <inheritdoc/>
    public override string Name => "Revolver-X";

    /// <inheritdoc/>
    public override string Description => "強化されたリボルバー。ある博士の特注品らしい";

    /// <inheritdoc/>
    protected override float Damage => 50f;

    /// <inheritdoc/>
    protected override int InitialAmmo => 6;

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments => [AttachmentName.None];

    /// <inheritdoc/>
    protected override bool AllowAttachmentChanges => false;

    /// <inheritdoc/>
    protected override void Customize(Item item)
    {
        base.Customize(item);

        SetRecoil(item, Recoil);
    }
}
