using UnityEngine;
namespace AntiMeme.Items.Melee;

/// <summary>
/// 槍。素直に殴れる近接武器です。
/// </summary>
public sealed class Spear : MeleeWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Jailbird;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;

    /// <inheritdoc/>
    public override string Name => "槍";

    /// <inheritdoc/>
    public override string Description => "間合いの長い刺突武器。癖がなく扱いやすい。";

    /// <inheritdoc/>
    protected override float MeleeDamage => 50f;
}
