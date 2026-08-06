using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>弾頭状態を音声層へ渡す警報灯です。</summary>
public sealed class AlarmLight : ObjectPrefab
{
    public bool IsAlarm { get; private set; }
    public Color NormalColor { get; set; } = Color.white;
    public Color AlarmColor { get; set; } = Color.red;
    public float Intensity { get; set; } = 1f;
    public static event Action<bool> AlarmStateChanged;
    protected override string SchematicName => "AlarmLight";

    public static void SetAlarmState(bool active)
    {
        foreach (AlarmLight light in ObjectPrefab.All.OfType<AlarmLight>()) light.SetState(active);
        AlarmStateChanged?.Invoke(active);
    }

    protected override void OnCreate()
    {
        IsSaveable = false;
    }
    protected override void OnSetup() => SetState(IsAlarm);
    private void SetState(bool active)
    {
        IsAlarm = active;
        try { Schematic?.AnimationController.Play(active ? "Alarm" : "Idle"); } catch { }
    }
}
