using System;
using AntiMeme.Items;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Scp914;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Nvg;

/// <summary>標準の緑色 NVG です。</summary>
public sealed class NvgNormal : CustomNvg, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.To<NvgRed>(),
        VeryFine = Scp914Rule.To<NvgBlue>(),
    };

    public override string Name => "ナイトビジョンゴーグル";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.6f, 1f, 0.6f);
    public override string Description => "遠くや暗い場所まで見えるようになる暗視ゴーグル。電池を消費する。";
}
