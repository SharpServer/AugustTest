using AntiMeme.Items.Bases;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
using UnityEngine;
using Utils;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// GOC高出力指向性電磁照射兵装《HID-Ω》。MicroHID を GoC が改修した高出力モデルです。
/// </summary>
/// <remarks>
/// 土台が MicroHID なので銃器モジュールを持ちません。ダメージ・命中は
/// <see cref="CustomWeapon"/> が「撃った本人が今持っているアイテム」で拾うため、
/// 銃かどうかに関係なく効きます。
/// </remarks>
public sealed class GunGoCTurret : CustomWeapon
{
    private const float BeamDamage = 100f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.MicroHID;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(255, 195, 75, 255);

    /// <inheritdoc/>
    public override string Name => "GOC高出力指向性電磁照射兵装《HID-Ω》";

    /// <inheritdoc/>
    public override string Description =>
        "<size=22>財団製MicroHIDをベースに、世界オカルト連合（GOC）が独自に大幅改修を施した高出力モデル。\n" +
        "出力制御系および冷却機構は戦闘効率を最優先に再設計されており、原型機に存在した安全制限の大半は意図的に解除されている。\n" +
        "これにより照射出力と持続時間は飛躍的に向上したが、使用者および周辺環境への負荷も著しく増大している。\n" +
        "対異常存在の強制無力化を主目的とした、極めて攻撃的な運用思想のもと開発された兵装である。\n" +
        "<color=red>高出力連続照射型：使用中はエネルギーを急速消費／過熱時、強制停止および使用者へダメージ</color></size>";

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1.15f, 1f, 1.15f);

    /// <inheritdoc/>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler is not StandardDamageHandler handler) return;

        handler.Damage = BeamDamage;

        ExplosionUtils.ServerSpawnEffect(ev.Player.Position, ItemType.GrenadeHE);
    }
}
