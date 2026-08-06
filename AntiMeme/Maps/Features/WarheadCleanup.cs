using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features.Warhead;

public sealed class WarheadCleanup : MapFeature
{
    protected override void Reset()
    {
        OmegaWarhead.Reset();
        WarheadBoomEffectHandler.Stop();
        WarheadDoorLockdown.UnlockAll();
    }
}
