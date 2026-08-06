using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features;

/// <summary>マップ由来の初期 ObjectPrefab をポイントから一度だけ出します。</summary>
public sealed class ObjectBootstraps : MapFeature
{
    public override void RegisterEvents() => ServerEvents.RoundStarted += Spawn;
    public override void UnregisterEvents() => ServerEvents.RoundStarted -= Spawn;
    private static void Spawn()
    {
        if (MapPoints.TryGet("EzPcTentaclePoint", out Vector3 tentacle))
            new Tentacle { Position = tentacle, IsSaveable = false }.Create();
        if (MapPoints.TryGet("HczOverbeyondDocumentPoint", out Vector3 document))
            new Document { Position = document, DocumentType = DocumentType.Overbeyond }.Create();
    }
}
