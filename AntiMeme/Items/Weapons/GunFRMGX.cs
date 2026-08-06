using AntiMeme.Items.Bases;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// FRMG-X。財団が無理を通して購入した最新式の FRMG-0 です。
/// </summary>
public sealed class GunFRMGX : CustomWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunFRMG0;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 0, 180, 255);

    /// <inheritdoc/>
    public override string Name => "FRMG-X";

    /// <inheritdoc/>
    public override string Description => "財団の無理を押し通して購入された最新式の FRMG-0。全体的に強化されている。";

    /// <inheritdoc/>
    protected override float Damage => 38f;

    /// <inheritdoc/>
    protected override int MagazineSize => 130;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1.08f, 1f, 1.35f);
}
