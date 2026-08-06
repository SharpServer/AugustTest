using System.Collections.Generic;
using MEC;
using UnityEngine;

using LightSourceToy = LabApi.Features.Wrappers.LightSourceToy;

#pragma warning disable CS0618 // 旧 LightShape も欠落なく複製する。

namespace AntiMeme.Maps.Objects;

/// <summary>
/// 調光できる光源です。<see cref="IsOn"/> と <see cref="Level"/> に応じて
/// <see cref="LightColor"/> と黒の間をなめらかに遷移させます。
/// </summary>
/// <remarks>
/// スキマティックは使いません。<see cref="LightSourceToy"/> を 1 個持ち、
/// 点灯 / 消灯は UnSpawn ではなく色の変更だけで表現します
/// (UnSpawn するとクライアント側のライトが一瞬飛ぶため)。
/// </remarks>
public sealed class ControllableLight : ObjectPrefab
{
    private const float MinTransitionDuration = 0.02f;

    private LightSourceToy light;
    private CoroutineHandle transition;
    private Color currentColor = Color.black;

    private bool isOn = true;
    private int level = 100;
    private Color lightColor = Color.white;
    private float intensity = 1f;
    private float range = 10f;
    private LightShadows shadowType = LightShadows.None;
    private float shadowStrength = 1f;
    private LightType lightType = LightType.Point;
    private LightShape lightShape = LightShape.Cone;
    private float spotAngle = 50f;
    private float innerSpotAngle = 40f;

    /// <summary>点いているかどうか。</summary>
    public bool IsOn
    {
        get => isOn;
        set
        {
            if (isOn == value) return;

            isOn = value;
            BeginTransition();
        }
    }

    /// <summary>点灯度合い (0-100)。100 で <see cref="LightColor"/> そのもの、0 に近づくほど黒に寄ります。</summary>
    public int Level
    {
        get => level;
        set
        {
            int clamped = Mathf.Clamp(value, 0, 100);

            if (level == clamped) return;

            level = clamped;
            BeginTransition();
        }
    }

    /// <summary>全点灯時の色です。</summary>
    public Color LightColor
    {
        get => lightColor;
        set
        {
            if (lightColor == value) return;

            lightColor = value;
            BeginTransition();
        }
    }

    /// <summary>色が変わりきるまでの秒数です。</summary>
    public float TransitionDuration { get; set; } = 1f;

    /// <summary>光の強さです。</summary>
    public float Intensity
    {
        get => intensity;
        set
        {
            intensity = value;

            if (light != null)
                light.Intensity = value;
        }
    }

    /// <summary>光の届く距離です。</summary>
    public float Range
    {
        get => range;
        set
        {
            range = value;

            if (light != null)
                light.Range = value;
        }
    }

    /// <summary>影の種類です。</summary>
    public LightShadows ShadowType
    {
        get => shadowType;
        set
        {
            if (shadowType == value) return;

            shadowType = value;

            if (light != null)
                light.ShadowType = value;
        }
    }

    /// <summary>影の濃さです。</summary>
    public float ShadowStrength
    {
        get => shadowStrength;
        set
        {
            shadowStrength = value;

            if (light != null)
                light.ShadowStrength = value;
        }
    }

    /// <summary>光源の種類です。</summary>
    public LightType LightType
    {
        get => lightType;
        set
        {
            if (lightType == value) return;

            lightType = value;

            if (light != null)
                light.Type = value;
        }
    }

    /// <summary>光源の形です。</summary>
    public LightShape LightShape
    {
        get => lightShape;
        set
        {
            if (lightShape == value) return;

            lightShape = value;

            if (light != null)
                light.Shape = value;
        }
    }

    /// <summary>スポットライトの外側の角度です。</summary>
    public float SpotAngle
    {
        get => spotAngle;
        set
        {
            spotAngle = value;

            if (light != null)
                light.SpotAngle = value;
        }
    }

    /// <summary>スポットライトの内側の角度です。</summary>
    public float InnerSpotAngle
    {
        get => innerSpotAngle;
        set
        {
            innerSpotAngle = value;

            if (light != null)
                light.InnerSpotAngle = value;
        }
    }

    private Color TargetColor => IsOn ? Color.Lerp(Color.black, LightColor, Level / 100f) : Color.black;

    /// <inheritdoc/>
    protected override void OnCreate()
    {
        currentColor = TargetColor;

        light = LightSourceToy.Create(Position, Rotation, Scale, networkSpawn: false);
        light.Intensity = Intensity;
        light.Range = Range;
        light.Color = currentColor;
        light.ShadowType = ShadowType;
        light.ShadowStrength = ShadowStrength;
        light.Type = LightType;
        light.Shape = LightShape;
        light.SpotAngle = SpotAngle;
        light.InnerSpotAngle = InnerSpotAngle;
        light.Spawn();
    }

    /// <inheritdoc/>
    protected override void OnTransformUpdated()
    {
        if (light == null) return;

        light.Position = Position;
        light.Rotation = Rotation;
        light.Scale = Scale;
    }

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        Timing.KillCoroutines(transition);

        if (light == null) return;

        light.Destroy();
        light = null;
    }

    private void BeginTransition()
    {
        // Create 前 (Options 適用時など) はライトがまだ無い。OnCreate 側が現在値を反映する。
        if (light == null) return;

        Timing.KillCoroutines(transition);
        transition = Timing.RunCoroutine(Transition(TargetColor));
    }

    private IEnumerator<float> Transition(Color target)
    {
        Color start = currentColor;
        float duration = Mathf.Max(MinTransitionDuration, TransitionDuration);

        for (float elapsed = 0f; elapsed < duration; elapsed += Timing.DeltaTime)
        {
            currentColor = Color.Lerp(start, target, Mathf.Clamp01(elapsed / duration));

            if (light != null)
                light.Color = currentColor;

            yield return Timing.WaitForOneFrame;
        }

        currentColor = target;

        if (light != null)
            light.Color = currentColor;
    }
}
