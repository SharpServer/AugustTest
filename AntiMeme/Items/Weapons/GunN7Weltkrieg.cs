using AntiMeme.Items.Bases;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// Nu-7 Weltkrieg 級軽機関銃。Nu-7 Marshal の得物です。
/// </summary>
public sealed class GunN7Weltkrieg : CustomWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunFRMG0;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(255, 195, 75, 255);

    /// <inheritdoc/>
    public override string Name => "Nu7 Weltkrieg 級軽機関銃";

    /// <inheritdoc/>
    public override string Description => "Nu-7 Marshal が使用するとても強い軽機関銃。威厳を感じさせる。";

    /// <inheritdoc/>
    protected override float Damage => 40f;

    /// <inheritdoc/>
    protected override int MagazineSize => 100;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1.15f, 1.3f, 1.25f);
}
