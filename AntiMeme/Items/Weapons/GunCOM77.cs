using AntiMeme.Items.Bases;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// COM-77。COM-18 を強化しただけの拳銃です。
/// </summary>
public sealed class GunCOM77 : CustomWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunCOM18;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;

    /// <inheritdoc/>
    public override string Name => "COM-77";

    /// <inheritdoc/>
    protected override float Damage => 25f;

    /// <inheritdoc/>
    protected override int MagazineSize => 7;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.15f);
}
