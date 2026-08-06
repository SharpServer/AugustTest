using UnityEngine;
namespace AntiMeme.Items.Melee;

/// <summary>
/// バトルアックス。一撃は重いが、次の一振りまで間が空きます。
/// </summary>
/// <remarks>
/// 旧実装はクールダウンを <c>static HashSet&lt;int&gt;</c> (プレイヤー ID) と、
/// 1 秒ごとに数えるコルーチンで持っていました。アイテム 1 個 = 1 インスタンスなので、
/// <see cref="MeleeWeapon.AttackCooldown"/> に秒数を書くだけで済みます。
/// またダメージも命中イベントで上書きせず、アイテムの値として焼いています
/// (旧実装は溜め攻撃や斧と無関係なダメージまで 200 に書き換えていました)。
/// </remarks>
public sealed class BattleAxe : MeleeWeapon
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Jailbird;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;

    /// <inheritdoc/>
    public override string Name => "バトルアックス";

    /// <inheritdoc/>
    public override string Description => "一撃で仕留めるための戦斧。重く、振り直しには時間が要る。";

    /// <inheritdoc/>
    protected override float MeleeDamage => 200f;

    /// <inheritdoc/>
    protected override float AttackCooldown => 5f;

    /// <inheritdoc/>
    protected override string CooldownHint => "<size=23>斧を振り直すには時間が要る。</size>";
}
