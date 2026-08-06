using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
using UnityEngine;
using Utils;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// GOC戦略兵装 EMR-X1。単発の電磁加速砲です。出力制限モデル。
/// </summary>
/// <remarks>
/// 旧実装は「ダメージを 0 にしてから <c>Hurt</c> で撃ち直す」形だったので、
/// 自分のダメージイベントを自分で踏んで無限再帰し、それを防ぐ処理中フラグを持っていました。
/// <b>ダメージ量をその場で書き換えるだけ</b>にすれば撃ち直しも再帰もフラグも要りません。
/// </remarks>
public sealed class GunGoCRailgun : CustomWeapon, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.ParticleDisruptor),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.Keep,
        VeryFine = Scp914Rule.To<GunGoCRailgunFull>(),
    };

    private const float ShotDamage = 2000f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.ParticleDisruptor;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(255, 195, 75, 255);

    /// <inheritdoc/>
    public override string Name => "GOC戦略兵装 EMR-X1";

    /// <inheritdoc/>
    public override string Description =>
        "<size=22>GoCのホワイトスーツに搭載予定だった主砲を、財団との協定に基づき歩兵運用向けへ再設計した電磁加速兵装。\n" +
        "対異常存在への対処能力を維持しつつ、安全性と携行性を重視した出力制限モデルであり、\n" +
        "制式採用機に比べ抑制された性能で運用される。\n" +
        "高エネルギー電磁加速機構により、小型ながら高い貫通力を発揮する。\n" +
        "<color=red>単発式：装填弾数1発のみ／最大6000ダメージの致死級出力</color></size>";

    /// <inheritdoc/>
    protected override int MagazineSize => 1;

    /// <inheritdoc/>
    protected override int InitialAmmo => 1;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1.15f, 1f, 1.15f);

    /// <inheritdoc/>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.DamageHandler is not StandardDamageHandler handler) return;

        handler.Damage = ShotDamage;

        ExplosionUtils.ServerSpawnEffect(ev.Player.Position, ItemType.GrenadeHE);
        ev.Attacker?.SendHitMarker();
    }
}
