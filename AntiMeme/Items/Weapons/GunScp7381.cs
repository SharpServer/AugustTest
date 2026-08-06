using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// SCP-7381。弾がほぼ尽きないパーティクル・ディスラプターです。
/// </summary>
/// <remarks>
/// ディスラプターだけは基底の <see cref="CustomWeapon.Damage"/> が効きません。
/// <see cref="DisruptorHitregModule.BaseDamage"/> の getter が単発 / 連射それぞれの
/// 専用フィールドを返すので、書き込んだ基底の値は読み戻されないためです。
/// </remarks>
public sealed class GunScp7381 : CustomWeapon, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.To<GunGoCRailgunFull>(),
        VeryFine = Scp914Rule.Custom(Scp914Detonation.Blast),
    };

    private const float ShotDamage = 35f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.ParticleDisruptor;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;

    /// <inheritdoc/>
    public override string Name => "SCP-7381";

    /// <inheritdoc/>
    public override string Description => "W.I.P";

    /// <inheritdoc/>
    protected override int MagazineSize => 999;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(3f, 1f, 1.15f);

    /// <inheritdoc/>
    protected override void Customize(Item item)
    {
        base.Customize(item);

        if (item is not FirearmItem firearm) return;

        foreach (ModuleBase module in firearm.Modules)
        {
            if (module is not DisruptorHitregModule disruptor) continue;

            disruptor._singleShotBaseDamage = ShotDamage;
            disruptor._rapidFireBaseDamage = ShotDamage;
        }
    }
}
