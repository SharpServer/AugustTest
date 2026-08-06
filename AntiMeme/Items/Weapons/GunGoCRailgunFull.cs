using AntiMeme.Items.Bases;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
using UnityEngine;
using Utils;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// GOC戦略兵装 EMR-01。制式採用された全出力の電磁加速砲です。1 射で 5 発消費します。
/// </summary>
/// <remarks>
/// <see cref="GunGoCRailgun"/> と同じく、旧実装の「0 にして撃ち直す + 再帰防止フラグ」は
/// ダメージ量の直接書き換えに置き換えています。
/// </remarks>
public sealed class GunGoCRailgunFull : CustomWeapon
{
    private const float ShotDamage = 5000f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.ParticleDisruptor;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(255, 195, 75, 255);

    /// <inheritdoc/>
    public override string Name => "GOC戦略兵装 EMR-01";

    /// <inheritdoc/>
    public override string Description =>
        "<size=22>世界オカルト連合（GOC）により正式採用された戦略級電磁加速兵装「EMR-01」。\n" +
        "対大規模異常存在の迅速な無力化を目的として設計されており、\n" +
        "超高出力の電磁加速機構により、単発で圧倒的な貫通力と破壊力を発揮する。\n" +
        "本兵装は複数弾を同時に消費しエネルギーを集約することで最大出力を実現しており、携行兵装としては規格外の性能を有する。\n" +
        "運用には厳格な安全プロトコルと適合装備（ホワイトスーツ）が必須とされる。\n" +
        "<color=red>単発式：発射ごとに弾薬を5発消費／最大出力時、15000ダメージを与える</color></size>";

    /// <inheritdoc/>
    protected override int MagazineSize => 20;

    /// <inheritdoc/>
    protected override int InitialAmmo => 20;

    /// <inheritdoc/>
    protected override int AmmoDrain => 5;

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
