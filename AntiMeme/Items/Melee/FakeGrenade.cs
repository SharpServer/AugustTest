using Footprinting;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>
/// 多目的グレネード。ドアや物は吹き飛ばしますが、人体には当たりません。
/// ただし 11 個に 1 個は不良品で、普通の手榴弾として炸裂します。
/// </summary>
/// <remarks>
/// 旧実装は炸裂そのものはバニラに任せて対象リストだけ空にしていました。
/// LabAPI の炸裂イベントに対象リストは無いので、爆発を差し止めてドアの破壊だけ自分で呼んでいます
/// (<see cref="ExplosionGrenade.ExplodeDoor"/> はゲーム側が公開しているのでそのまま使えます)。
/// </remarks>
public sealed class FakeGrenade : ThrownGrenade
{
    private const int DefectOdds = 11;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GrenadeHE;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;

    /// <inheritdoc/>
    public override string Name => "多目的グレネード";

    /// <inheritdoc/>
    public override string Description =>
        "様々な清掃やドア破壊等、多種多様な用途に使う特殊グレネード。\n人体等に被害はないらしい";

    /// <inheritdoc/>
    protected override float FuseTime => 0.5f;

    /// <inheritdoc/>
    protected override void OnExploding(ProjectileExplodingEventArgs ev)
    {
        if (Random.Range(0, DefectOdds) == 0)
        {
            ev.Player?.SendHint("<color=red><size=32>不良品だった！！！</size></color>", 5f);

            return;
        }

        Suppress(ev);

        if (ev.TimedGrenade.Base is not ExplosionGrenade settings) return;

        Footprint attacker = new Footprint(ev.Player?.ReferenceHub);

        foreach (Door door in Door.List)
        {
            if ((door.Position - ev.Position).sqrMagnitude > settings.MaxRadius * settings.MaxRadius) continue;

            ExplosionGrenade.ExplodeDoor(door.Base, ev.Position, settings, attacker);
        }
    }
}
