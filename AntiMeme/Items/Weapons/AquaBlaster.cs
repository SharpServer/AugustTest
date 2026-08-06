using AntiMeme.Items.Bases;
using AntiMeme.Abilities;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;
using Utils;

namespace AntiMeme.Items.Weapons;

/// <summary>命中した相手を沈めて減速させる水鉄砲です。</summary>
public sealed class AquaBlaster : CustomWeapon, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.ToVanilla(ItemType.GunE11SR),
        Fine = Scp914Rule.To<HydroCannon>(),
        VeryFine = Scp914Rule.Custom(context =>
        {
            Vector3 position = context.OutputPosition;
            context.Consume();
            ExplosionUtils.ServerSpawnEffect(position, ItemType.GrenadeFlash);
        }),
    };

    private const byte SinkholeIntensity = 45;
    private const float SinkholeDuration = 10f;
    private const byte SlownessIntensity = 25;
    private const float SlownessDuration = 6f;
    private static readonly Color WaterColor = new Color(0.25f, 0.85f, 1f, 0.55f);

    public override ItemType BaseType => ItemType.GunCOM18;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.25f, 0.85f, 1f, 0.55f);
    public override string Name => "アクア・ブラスター";
    public override string Description => "当てた相手をずぶ濡れにし、しばらく Sinkhole へ引きずり込む水鉄砲。";
    protected override float Damage => 14f;
    protected override int MagazineSize => 20;
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.05f);

    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.Player is not { IsDestroyed: false } target || !ev.IsAllowed) return;

        target.EnableEffect<Sinkhole>(SinkholeIntensity, SinkholeDuration);
        target.EnableEffect<Slowness>(SlownessIntensity, SlownessDuration);

        if (ev.Attacker is { } attacker)
        {
            Vector3 origin = attacker.Camera.position;
            Vector3 impact = target.Position;
            for (int i = 1; i <= 5; i++)
                AbilityVisuals.Spawn(PrimitiveType.Sphere, Vector3.Lerp(origin, impact, i / 5f), Vector3.one * .12f, WaterColor, .25f);
        }
    }
}
