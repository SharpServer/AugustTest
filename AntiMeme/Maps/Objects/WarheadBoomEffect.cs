using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>弾頭爆発時の煙スキマティックです。</summary>
public sealed class WarheadBoomEffect : ObjectPrefab
{
    public float RiseDuration { get; set; } = 5f;
    public float RiseDistance { get; set; } = 20f;
    protected override string SchematicName => "WarheadBoomEffect";
    protected override void OnCreate()
    {
        Vector3 start = Position;
        Delay(Mathf.Max(.1f, RiseDuration), () => Position = start + Vector3.up * RiseDistance);
    }
}
