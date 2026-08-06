using AntiMeme.Items.Bases;
using System;
using System.Collections.Generic;
using AntiMeme.Abilities;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>命中した相手を強く吹き飛ばす高圧放水砲です。</summary>
public sealed class HydroCannon : CustomWeapon, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<AquaBlaster>(),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.ToVanilla(ItemType.ParticleDisruptor),
        VeryFine = Scp914Rule.Destroy,
    };

    private const byte SinkholeIntensity = 60;
    private const float SinkholeDuration = 14f;
    private const float KnockbackPower = 6f;
    private const float UpwardPower = 1.5f;
    private const float KnockbackDuration = .15f;
    private const float ShotMemorySeconds = .35f;
    private static readonly Color WaterColor = new Color(.2f, .75f, 1f, .5f);
    private readonly Dictionary<(int Attacker, int Target), ShotImpulse> recentShots = new();

    public override ItemType BaseType => ItemType.GunFRMG0;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.2f, 0.75f, 1f, 0.5f);
    public override string Name => "ハイドロ・キャノン";
    public override string Description => "直撃した相手を吹き飛ばし、長時間 Sinkhole へ引きずり込む高圧放水砲。";
    protected override float Damage => 6f;
    protected override int MagazineSize => 40;
    protected override Vector3 Scale => new Vector3(1.05f, 1f, 1.3f);

    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (!ev.IsAllowed || ev.Attacker is not { IsDestroyed: false } attacker || ev.Player is not { IsDestroyed: false } target)
            return;

        if (attacker.IsSCP == target.IsSCP) return;

        target.EnableEffect<Sinkhole>(SinkholeIntensity, SinkholeDuration);
        Vector3 direction = recentShots.TryGetValue((attacker.PlayerId, target.PlayerId), out ShotImpulse shot) && shot.ExpiresAt >= Time.time
            ? shot.Direction
            : attacker.Camera.forward;
        recentShots.Remove((attacker.PlayerId, target.PlayerId));
        direction.Normalize();
        direction.y = Mathf.Max(direction.y, UpwardPower / KnockbackPower);
        FpcPush.Impulse(target, direction * KnockbackPower, KnockbackDuration);
    }

    protected override void OnShot()
    {
        if (Owner is not { IsDestroyed: false } owner) return;

        Vector3 position = owner.Camera.position + owner.Camera.forward;
        for (int i = 0; i < 8; i++)
            AbilityVisuals.Spawn(PrimitiveType.Sphere, position + UnityEngine.Random.insideUnitSphere * .3f,
                Vector3.one * (.25f + i * .1f), WaterColor, .3f);
    }

    protected override void OnReleased() => recentShots.Clear();

    private readonly struct ShotImpulse
    {
        public ShotImpulse(Vector3 direction, float expiresAt)
        {
            Direction = direction;
            ExpiresAt = expiresAt;
        }

        public Vector3 Direction { get; }
        public float ExpiresAt { get; }
    }
}
