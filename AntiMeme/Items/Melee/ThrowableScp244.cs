using AntiMeme.Items.Scp914;
using InventorySystem.Items.Usables.Scp244;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;
using Sliced.API.Features.Attributes;

namespace AntiMeme.Items.Melee;

/// <summary>
/// SCP-244 (投擲)。投げた先で寒霧が展開します。
/// </summary>
/// <remarks>
/// <para>
/// 投げた瞬間に SCP-244 の実体を作り、飛んでいく弾体に追従させます。
/// 着弾してから出すだけにすると、投げている間ずっと霧が見えず、
/// どこへ飛んだのかも分からないただのフラググレネードになります。
/// </para>
/// <para>
/// 追従は <b>transform の親子付け</b>で行います。旧実装は
/// <c>static Dictionary&lt;Projectile, …&gt;</c> 2 本と毎フレームの Lerp コルーチンで
/// 位置を追わせていましたが、弾体に括り付けてしまえば同じことです。
/// 炸裂の瞬間に親を外して物理を戻し、その場に置いていきます。
/// </para>
/// </remarks>
[LegacyName("Scp244-Throwable")]
public sealed class ThrowableScp244 : ThrownGrenade, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Weighted(
            (0.5f, Scp914Rule.ToVanilla(ItemType.SCP244a)),
            (0.5f, Scp914Rule.ToVanilla(ItemType.SCP244b))),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.Destroy,
        VeryFine = Scp914Rule.Destroy,
    };

    private const float ActiveDuration = 5f;

    /// <summary>弾体の少し前に出します。弾体の原点と重ねると位置がずれて見えます。</summary>
    private const float SpawnForwardOffset = 0.5f;

    /// <summary>飛んでいる間、弾体に括り付けてある霧です。</summary>
    private Scp244Pickup carried;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GrenadeFlash;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;

    /// <inheritdoc/>
    public override string Name => "SCP-244 (投擲)";

    /// <inheritdoc/>
    public override string Description => "投擲して使用することができるSCP-244";

    /// <summary>
    /// 投げた瞬間に霧の実体を作り、弾体にぶら下げます。
    /// </summary>
    protected override void OnThrown(PlayerThrewProjectileEventArgs ev)
    {
        ItemType type = Random.Range(0, 2) == 0 ? ItemType.SCP244a : ItemType.SCP244b;
        Transform projectile = ev.Projectile.Transform;

        if (Pickup.Create(
                type,
                projectile.position + projectile.forward * SpawnForwardOffset,
                projectile.rotation) is not Scp244Pickup scp244)
        {
            return;
        }

        scp244.Spawn();

        // 弾体の子にして物理を止める。以降は弾体が動くだけで付いてくる。
        scp244.Transform.SetParent(projectile);
        scp244.Transform.localPosition = Vector3.zero;
        scp244.Transform.localRotation = Quaternion.identity;

        if (scp244.Rigidbody is { } body)
            body.isKinematic = true;

        carried = scp244;
    }

    /// <inheritdoc/>
    protected override void OnExploding(ProjectileExplodingEventArgs ev)
    {
        Suppress(ev);

        // 投げた時点の霧をその場に置いていく。作り直すと一瞬消えて見える。
        if (carried is not { IsDestroyed: false } scp244)
            return;

        carried = null;

        scp244.Transform.SetParent(null);
        scp244.Position = ev.Position;

        if (scp244.Rigidbody is { } body)
            body.isKinematic = false;

        scp244.State = Scp244State.Active;

        RoundScope.Current.Delay(ActiveDuration, () =>
        {
            if (scp244 is { IsDestroyed: false })
                scp244.State = Scp244State.Destroyed;
        });
    }

    /// <summary>投げたまま炸裂しなかった場合 (ラウンド終了・強制破棄) の後片付けです。</summary>
    protected override void OnReleased()
    {
        if (carried is { IsDestroyed: false } scp244)
            scp244.Destroy();

        carried = null;

        base.OnReleased();
    }
}
