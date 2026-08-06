using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features;

public sealed class Scp513Runtime : MapFeature
{
    public override void RegisterEvents() => LabApi.Events.Handlers.ServerEvents.RoundStarted += Scp513.Start;
    public override void UnregisterEvents() => LabApi.Events.Handlers.ServerEvents.RoundStarted -= Scp513.Start;
    protected override void Reset() => Scp513.Stop();
}
