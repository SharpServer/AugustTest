using System;
using System.Linq;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>触手の見た目・攻撃周期を共有する基底です。能力層はイベントで連携します。</summary>
public abstract class TentacleBase : ObjectPrefab
{
    private float nextAttack;
    public float MaxHealth { get; set; } = 100f;
    public float Health { get; private set; }
    public float AttackRange { get; set; } = 4f;
    public float AttackDamage { get; set; } = 20f;
    public float AttackInterval { get; set; } = 2f;
    public float StrikeDuration { get; set; } = .75f;
    public bool Enabled { get; set; } = true;
    protected abstract string TentacleSchematicName { get; }

    protected override string SchematicName => TentacleSchematicName;

    /// <summary>
    /// 見た目だけです。攻撃判定は <see cref="Tick"/> の距離判定が持つので、
    /// モデルに当たり判定を持たせるとプレイヤーが触手に引っ掛かります。
    /// </summary>
    protected override bool SchematicCollides => false;

    protected override void OnCreate()
    {
        Health = Mathf.Max(1f, MaxHealth);
        nextAttack = Time.time + AttackInterval;
        Loop(.1f, Tick);
    }

    protected override void OnSetup()
    {
        if (GetInteractable("Interactable") is { } handle)
            handle.Interacted += ev => MapSignals.RaiseTentacleInteracted(ev.Player, this);
    }

    public bool Damage(float amount, Player source = null)
    {
        if (amount <= 0f || !IsAlive) return false;
        Health = Mathf.Max(0f, Health - amount);
        if (Health <= 0f) Destroy();
        return true;
    }

    private void Tick()
    {
        if (!Enabled || Time.time < nextAttack) return;
        Player target = Player.ReadyList
            .Where(player => player is { IsDestroyed: false, IsAlive: true })
            .OrderBy(player => (player.Position - Position).sqrMagnitude)
            .FirstOrDefault(player => Vector3.Distance(player.Position, Position) <= AttackRange);
        if (target is null) return;
        nextAttack = Time.time + Mathf.Max(.1f, AttackInterval);
        try { Schematic?.AnimationController.Play("attacking"); } catch { }
        target.Damage(AttackDamage, "Tentacle");
        MapSignals.RaiseTentacleInteracted(target, this);
        Delay(StrikeDuration, () => { try { Schematic?.AnimationController.Play("idle"); } catch { } });
    }
}

public sealed class Tentacle : TentacleBase
{
    protected override string TentacleSchematicName => "Tentacle";
}

public sealed class WaterTentacle : TentacleBase
{
    protected override string TentacleSchematicName => "WaterTentacle";
    public float SlowDuration { get; set; } = 2f;
}
