using AntiMeme.Items.Bases;
using CameraShaking;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// FSP-18。反動を抑え込んだ FSP-9 の改良版です。
/// </summary>
public sealed class GunFSP18 : CustomWeapon
{
    private static readonly RecoilSettings TamedRecoil = new RecoilSettings(0.08f, 0.12f, 0.75f, 0.11f, 0.15f);

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunFSP9;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;

    /// <inheritdoc/>
    public override string Name => "FSP-18";

    /// <inheritdoc/>
    public override string Description =>
        "財団が開発した FSP-9 の改良版。\n" +
        "反動の制御に優れており主に上級警備隊に配備されている。";

    /// <inheritdoc/>
    protected override float Damage => 27f;

    /// <inheritdoc/>
    protected override int MagazineSize => 42;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.62f);

    /// <inheritdoc/>
    protected override void Customize(Item item)
    {
        base.Customize(item);
        SetRecoil(item, TamedRecoil);
    }
}
