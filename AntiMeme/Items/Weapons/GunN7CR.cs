using AntiMeme.Items.Bases;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// MTF-N7-CR。Nu-7 Commander が使う小銃です。
/// </summary>
public sealed class GunN7CR : CustomWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunE11SR;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;

    /// <inheritdoc/>
    public override string Name => "MTF-N7-CR";

    /// <inheritdoc/>
    public override string Description => "Nu-7 Commander が使用する銃。";

    /// <inheritdoc/>
    protected override float Damage => 40f;

    /// <inheritdoc/>
    protected override int MagazineSize => 100;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.15f);
}
