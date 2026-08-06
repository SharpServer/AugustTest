using AntiMeme.Items.Bases;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// Logicer SUPER。装弾数を極端に増やしたロジサーです。
/// </summary>
public sealed class GunSuperLogicer : CustomWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunLogicer;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 75, 0, 255);

    /// <inheritdoc/>
    public override string Name => "Logicer SUPER";

    /// <inheritdoc/>
    public override string Description => "最新式の Logicer。SUPER に強化されている。";

    /// <inheritdoc/>
    protected override float Damage => 30f;

    /// <inheritdoc/>
    protected override int MagazineSize => 255;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1.08f, 1f, 1.35f);
}
