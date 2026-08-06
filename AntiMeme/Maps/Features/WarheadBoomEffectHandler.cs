using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features.Warhead;

public static class WarheadBoomEffectHandler
{
    public static bool IsBooming { get; private set; }
    public static void Start(Vector3 position) { IsBooming = true; WarheadBoomEffectUtil.CreateAndStartEffect(position); }
    public static void Stop() { IsBooming = false; WarheadBoomEffectUtil.StopAllEffects(); }
}
