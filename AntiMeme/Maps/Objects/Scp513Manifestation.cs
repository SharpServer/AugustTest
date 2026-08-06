using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>対象プレイヤーにだけ見える SCP-513 の追跡演出です。</summary>
public sealed class Scp513Manifestation : ObjectPrefab
{
    public Player TargetPlayer { get; set; }
    protected override string SchematicName => "SCP513";
    protected override float SetupDelay => 0f;
    protected override void OnSetup()
    {
        if (TargetPlayer is not { IsDestroyed: false } || Schematic is null) { Destroy(); return; }
        Schematic.transform.SetParent(TargetPlayer.GameObject?.transform, true);
    }
}
