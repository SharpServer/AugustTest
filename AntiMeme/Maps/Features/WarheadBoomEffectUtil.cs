using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features.Warhead;

public static class WarheadBoomEffectUtil
{
    private static readonly List<WarheadBoomEffect> Active = new List<WarheadBoomEffect>();
    public static void CreateAndStartEffect(Vector3 position, float endTime = 10f, float spawnInterval = .15f, float randomRange = .03f)
    {
        WarheadBoomEffect effect = (WarheadBoomEffect)new WarheadBoomEffect { Position = position, Lifespan = endTime }.Create();
        Active.Add(effect);
    }
    public static void StopAllEffects() { foreach (WarheadBoomEffect effect in Active.ToArray()) effect?.Destroy(); Active.Clear(); }
}
