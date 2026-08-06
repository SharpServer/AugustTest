using AntiMeme.Items.Bases;
using System.Collections.Generic;
using AntiMeme.Abilities;
using AntiMeme.Items.Weapons;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>
/// XXX-CAPYBARA-XXX。弾は飛ばず、代わりに回転するカピバラが飛んでいきます。
/// </summary>
/// <remarks>
/// 旧実装は当たり判定に「一度当てた相手」の記録が無く、生存者全員を対象にしていました。
/// カピバラの中に立っていると毎フレーム 2 ダメージが入り続け、味方も撃った本人も巻き込まれていました。
/// </remarks>
public sealed class CapybaraMissile : CustomWeapon
{
    private const float FlightTime = 5f;
    private const float FlightDistance = 99f;
    private const float SpinSpeed = 1440f;
    private const float HitRadius = 1f;
    private const float HitDamage = 2f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunCOM18;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;

    /// <inheritdoc/>
    public override string Name => "XXX-CAPYBARA-XXX";

    /// <inheritdoc/>
    public override string Description => "HUBERT YEAAAAAAAAA";

    /// <summary>
    /// 弾そのものは当たっても痛くありません。痛いのはカピバラです。
    /// </summary>
    protected override float Damage => 0f;

    /// <inheritdoc/>
    protected override int MagazineSize => 5;

    /// <inheritdoc/>
    protected override Vector3 Scale => Vector3.one * 5f;

    /// <inheritdoc/>
    protected override void OnShot()
    {
        if (Owner is not { IsDestroyed: false } owner) return;

        Vector3 forward = owner.Camera.forward.normalized;
        Vector3 start = owner.Position + forward * 1.5f;

        CapybaraToy capybara = CapybaraToy.Create(start, Quaternion.LookRotation(forward));
        capybara.CollidersEnabled = false;

        PlayerScope.Of(owner).Track(Timing.RunCoroutine(Fly(owner, capybara, start, start + forward * FlightDistance)));
    }

    private static IEnumerator<float> Fly(Player shooter, CapybaraToy capybara, Vector3 start, Vector3 end)
    {
        HashSet<Player> hit = [];

        float elapsed = 0f;

        while (elapsed < FlightTime)
        {
            if (capybara.IsDestroyed) yield break;

            Vector3 position = Vector3.Lerp(start, end, elapsed / FlightTime);

            capybara.Position = position;
            capybara.Rotation = Quaternion.Euler(0f, SpinSpeed * elapsed, 0f);

            foreach (Player target in AbilityTargeting.EnemiesAround(shooter, position, HitRadius))
            {
                if (!hit.Add(target)) continue;

                target.Damage(HitDamage, shooter, (end - start).normalized * HitDamage);

                shooter.SendHitMarker();
            }

            elapsed += Time.deltaTime;

            yield return Timing.WaitForOneFrame;
        }

        if (!capybara.IsDestroyed)
            capybara.Destroy();
    }
}
