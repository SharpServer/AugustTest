using System;
using AntiMeme.Items;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Scp914;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Nvg;

/// <summary>赤色・無限電池 NVG です。</summary>
public sealed class NvgRed : CustomNvg
{
    public override string Name => "ナイトビジョンゴーグル - 赤";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;
    public override string Description => "遠くや暗い場所まで見えるようになる暗視ゴーグル。電池を消費しない。";

    protected override NvgProfile Profile => new()
    {
        DrainPerSecond = 0f,
        LightColor = Color.red,
        LightRange = 30f,
        LightIntensity = 10000f,
        WornBlindnessIntensity = 15,
        UseBlackout = false,
        Remove1344Effect = true,
    };
}
