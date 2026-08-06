using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features;

public sealed class TrashboxRuntime : MapFeature
{
    protected override void Reset() => Trashbox.ResetRoundState();
}
