using System;
using AntiMeme.Items;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Scp914;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Nvg;

/// <summary>NVG のライトと電池消費を定義するプロファイルです。</summary>
public readonly struct NvgProfile
{
    public float DrainPerSecond { get; init; }
    public Color LightColor { get; init; }
    public float LightRange { get; init; }
    public float LightIntensity { get; init; }
    public byte WornBlindnessIntensity { get; init; }
    public bool UseBlackout { get; init; }
    public bool Remove1344Effect { get; init; }

    public static NvgProfile Default => new()
    {
        DrainPerSecond = 1.85f,
        LightColor = new Color(0.6f, 1f, 0.6f),
        LightRange = 30f,
        LightIntensity = 10000f,
        WornBlindnessIntensity = CustomPlayerEffects.Blindness.MinIntensity,
        UseBlackout = true,
        Remove1344Effect = true,
    };
}
