using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>
/// Flashbang-E。SCP にだけ効くように改良されたフラッシュバンです。
/// </summary>
/// <remarks>
/// 目潰しの強さは距離と向きで決まる複雑な計算ですが、ゲーム側の
/// <see cref="FlashbangGrenade.ProcessPlayer"/> がそのまま公開されているので、
/// 炸裂だけ差し止めて対象を絞って呼び直しています。閃光の見え方はバニラと同じです。
/// </remarks>
public sealed class FlashBangE : ThrownGrenade
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GrenadeFlash;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;

    /// <inheritdoc/>
    public override string Name => "Flashbang-E";

    /// <inheritdoc/>
    public override string Description => "SCPオブジェクトにのみ当たるように改良されたフラッシュバン。";

    /// <inheritdoc/>
    protected override float FuseTime => 0.5f;

    /// <inheritdoc/>
    protected override void OnExploding(ProjectileExplodingEventArgs ev)
    {
        Suppress(ev);

        if (ev.TimedGrenade.Base is not FlashbangGrenade flashbang) return;

        flashbang._hitPlayerCount = 0;

        foreach (Player target in Player.ReadyList)
        {
            if (target.IsSCP && target.IsAlive)
                flashbang.ProcessPlayer(target.ReferenceHub);
        }

        if (flashbang._hitPlayerCount > 0)
            ev.Player?.SendHitMarker();
    }
}
