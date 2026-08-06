using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>HID の射程判定を行うマップ砲台です。NPC を作らず、攻撃判定だけを公開します。</summary>
public sealed class HIDTurretObject : ObjectPrefab
{
    public static bool IsPowerEnabled { get; private set; }
    public float TotalRange { get; set; } = 30f;
    public float Damage { get; set; } = 40f;
    public float FireInterval { get; set; } = .2f;
    private float nextFire;
    protected override string SchematicName => "HIDTurretSchem";
    protected override void OnCreate() => Loop(.05f, Tick);
    public static void EnablePower(bool enabled) => IsPowerEnabled = enabled;
    private void Tick()
    {
        if (!IsPowerEnabled || Time.time < nextFire) return;
        Player target = Player.ReadyList.Where(p => p is { IsDestroyed: false, IsAlive: true })
            .OrderBy(p => (p.Position - Position).sqrMagnitude).FirstOrDefault(p => Vector3.Distance(p.Position, Position) <= TotalRange);
        if (target is null) return;
        nextFire = Time.time + Mathf.Max(.02f, FireInterval);
        target.Damage(Damage, "HID Turret");
    }
}
