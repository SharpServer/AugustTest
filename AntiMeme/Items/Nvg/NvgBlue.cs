using System;
using AntiMeme.Items;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Scp914;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Nvg;

/// <summary>青色・長距離 NVG です。</summary>
public sealed class NvgBlue : CustomNvg
{
    public override string Name => "ナイトビジョンゴーグル - 青";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.blue;
    public override string Description => "遠くや暗い場所まで見えるようになる暗視ゴーグル。電池を消費するが、周りの情報が分かる。";

    protected override NvgProfile Profile => new()
    {
        DrainPerSecond = 3f,
        LightColor = Color.blue,
        LightRange = 180f,
        LightIntensity = 10000f,
        WornBlindnessIntensity = 15,
        UseBlackout = true,
        Remove1344Effect = false,
    };
}
