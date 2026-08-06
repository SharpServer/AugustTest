using UnityEngine;

namespace AntiMeme.Items.Nvg;

/// <summary>GoC の VERITAS 視覚支援装置です。</summary>
public sealed class Veritas : CustomNvg
{
    public override string Name => "VERITAS";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 0, 180, 255);
    public override string Description => "遠距離の敵性存在を認識する GoC 製視覚支援装置。";

    protected override NvgProfile Profile => new()
    {
        DrainPerSecond = 1.85f,
        LightColor = new Color(0.2f, 0.45f, 1f),
        LightRange = 30f,
        LightIntensity = 10000f,
        WornBlindnessIntensity = CustomPlayerEffects.Blindness.MinIntensity,
        UseBlackout = true,
        Remove1344Effect = false,
    };
}
