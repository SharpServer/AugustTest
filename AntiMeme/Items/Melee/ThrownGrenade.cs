using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using Utils;

namespace AntiMeme.Items.Melee;

/// <summary>
/// 投擲物の土台です。
///
/// <para>
/// 投げると弾体 (<see cref="TimedGrenadeProjectile"/>) が作られますが、
/// <b>シリアルは投げたアイテムから引き継がれる</b>ので追跡は切れません。
/// 炸裂の判定もシリアルだけで引けます。
/// </para>
/// <para>
/// ゲーム側は炸裂を差し止められると (<c>ProjectileExploding</c> のキャンセル)
/// 弾体の後片付けまで一緒に飛ばしてしまい、爆発しない置物が残ります。
/// ここで必ず片付けるので、派生は <see cref="Suppress"/> を呼ぶだけで済みます。
/// </para>
/// </summary>
public abstract class ThrownGrenade : CustomItem
{
    private static bool hooked;

    protected ThrownGrenade() => Hook();

    /// <summary>
    /// 投げてから炸裂するまでの秒数です。負値ならバニラのまま。
    /// </summary>
    protected virtual float FuseTime => -1f;

    /// <summary>
    /// 投げた直後に呼ばれます。
    /// </summary>
    protected virtual void OnThrown(PlayerThrewProjectileEventArgs ev)
    {
    }

    /// <summary>
    /// 炸裂する直前に呼ばれます。バニラの爆発を自前の効果に置き換えるなら
    /// <see cref="Suppress"/> を呼んでください。
    /// </summary>
    protected virtual void OnExploding(ProjectileExplodingEventArgs ev)
    {
    }

    /// <summary>
    /// バニラの爆発を差し止め、見た目の爆発だけ出します。弾体の片付けは呼び出し後に自動で行われます。
    /// </summary>
    protected static void Suppress(ProjectileExplodingEventArgs ev)
    {
        ev.IsAllowed = false;

        ExplosionUtils.ServerSpawnEffect(ev.Position, ev.TimedGrenade.Type);
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.ThrewProjectile += OnAnyThrewProjectile;
        ServerEvents.ProjectileExploding += OnAnyExploding;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.ThrewProjectile -= OnAnyThrewProjectile;
            ServerEvents.ProjectileExploding -= OnAnyExploding;
            hooked = false;
        });
    }

    private static void OnAnyThrewProjectile(PlayerThrewProjectileEventArgs ev)
    {
        if (Of(ev.Projectile.Serial) is not ThrownGrenade grenade) return;

        if (grenade.FuseTime >= 0f && ev.Projectile is TimedGrenadeProjectile timed)
            timed.RemainingTime = grenade.FuseTime;

        grenade.OnThrown(ev);
    }

    private static void OnAnyExploding(ProjectileExplodingEventArgs ev)
    {
        if (Of(ev.TimedGrenade.Serial) is not ThrownGrenade grenade) return;

        grenade.OnExploding(ev);

        // 差し止めた場合はゲーム側の DestroySelf も走らないので、ここで消す。
        if (!ev.IsAllowed)
            ev.TimedGrenade.Destroy();

        // 炸裂した投擲物は二度と戻ってこない。シリアルを抱え込まずに手放す。
        grenade.Release();
    }
}
