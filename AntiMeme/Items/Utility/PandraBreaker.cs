using System;
using System.Collections.Generic;
using AntiMeme.Items;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// Pandra Breaker。投擲操作で、登録済みの SCP-076 抑制装置を起動します。
/// </summary>
public sealed class PandraBreaker : CustomItem
{
    private bool cooldown;
    private CoroutineHandle cooldownHandle;

    public override ItemType BaseType => ItemType.Radio;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;
    public override string Name => "Pandra Breaker - アベル爆破スイッチ";
    public override string Description => "SCP-076を爆破する。\n投げて使用可能";

    protected override void OnReleased()
    {
        if (cooldownHandle.IsRunning)
            Timing.KillCoroutines(cooldownHandle);
        cooldownHandle = default;
        cooldown = false;
        base.OnReleased();
    }

    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw || Of(ev.Item.Serial) is not PandraBreaker breaker)
            return;

        ev.IsAllowed = false;
        if (breaker.cooldown)
        {
            ev.Player.SendHint("<size=23>現在クールダウン中です！使用してから一分後に再利用できるようになります。</size>", 4f);
            return;
        }

        PandraBreakerDetonationResult result = PandraBreakerIntegration.TryDetonate();
        if (!result.Activated)
        {
            ev.Player.SendHint(
                result.HasActiveTarget
                    ? "<size=23>SCP-076は存在しますが、まだ反逆状態ではありません。</size>"
                    : "<size=23>SCP-076が存在しません。</size>",
                5f);
            return;
        }

        breaker.cooldown = true;
        breaker.cooldownHandle = PlayerScope.Of(ev.Player).Delay(60f, _ =>
        {
            breaker.cooldown = false;
            breaker.cooldownHandle = default;
        });
    }

}


/// <summary>
/// SCP-076 役職層との狭い統合境界です。役職の再構築時に target を登録します。
/// </summary>
public static class PandraBreakerIntegration
{
    private static readonly List<IPandraBreakerTarget> Targets = new();

    public static IDisposable Register(IPandraBreakerTarget target)
    {
        if (target is null)
            throw new ArgumentNullException(nameof(target));

        Targets.Add(target);
        Registration registration = new(target);
        ItemRuntime.Register(registration.Dispose);
        return registration;
    }

    internal static PandraBreakerDetonationResult TryDetonate()
    {
        bool hasActive = false;
        bool activated = false;

        foreach (IPandraBreakerTarget target in Targets.ToArray())
        {
            if (!target.IsActive)
                continue;

            hasActive = true;
            if (target.IsResistanceState)
                activated |= target.TryDetonateSuppressionDevice();
        }

        return new PandraBreakerDetonationResult(hasActive, activated);
    }

    private sealed class Registration : IDisposable
    {
        private readonly IPandraBreakerTarget target;
        private bool disposed;

        public Registration(IPandraBreakerTarget target) => this.target = target;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Targets.Remove(target);
        }
    }
}

internal readonly struct PandraBreakerDetonationResult
{
    public PandraBreakerDetonationResult(bool hasActiveTarget, bool activated)
    {
        HasActiveTarget = hasActiveTarget;
        Activated = activated;
    }

    public bool HasActiveTarget { get; }
    public bool Activated { get; }
}
