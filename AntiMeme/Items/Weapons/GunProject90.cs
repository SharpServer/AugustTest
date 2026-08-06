using AntiMeme.Items.Bases;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// Project-90。クロスベクを土台にした古風なサブマシンガンです。
///
/// <para>
/// 旧実装は発砲音・空撃ち音・リロード音・消音時の音をすべて差し替えていました。
/// <b>音は保留</b>です。音声層 (担当 15) の再構築後にその上へ載せます。
/// </para>
/// </summary>
public sealed class GunProject90 : CustomWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunCrossvec;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.cyan;

    /// <inheritdoc/>
    public override string Name => "Project-90";

    /// <inheritdoc/>
    public override string Description => "昔ながらの、安定した撃ちどけ。";

    /// <inheritdoc/>
    protected override float Damage => 38f;

    /// <inheritdoc/>
    protected override int MagazineSize => 50;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1.135f, 1.3555f, 1.08f);
}
