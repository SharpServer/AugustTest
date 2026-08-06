using System;
using CustomPlayerEffects;
using HintServiceMeow.Core.Extension;
using HintServiceMeow.Core.Utilities;
using LabApi.Features.Wrappers;
using UnityEngine;

using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using Random = System.Random;

namespace AntiMeme.Effects;

/// <summary>
/// 発狂状態のうち、進行パターンの抽選と付随エフェクトの管理です。
/// 画面描画は <c>Insanity.Layers.cs</c>、フェーズ定義は <c>Insanity.Content.cs</c> にあります。
/// </summary>
public partial class Insanity
{
    /// <summary>
    /// 進行パターンを重み付きで抽選します。
    /// <see cref="ForcedPatternName"/> が指定されていればそれを優先します。
    /// </summary>
    private static TripPattern PickPattern()
    {
        TripPattern[] patterns = Patterns;

        if (patterns.Length == 0)
            return BuildDefaultPatterns()[0];

        if (!string.IsNullOrEmpty(ForcedPatternName) && TryGetPattern(ForcedPatternName, out TripPattern forced))
            return forced;

        float total = 0f;

        foreach (TripPattern candidate in patterns)
            total += Mathf.Max(0f, candidate.Weight);

        if (total <= 0f)
            return patterns[UnityEngine.Random.Range(0, patterns.Length)];

        float roll = UnityEngine.Random.Range(0f, total);

        foreach (TripPattern candidate in patterns)
        {
            roll -= Mathf.Max(0f, candidate.Weight);

            if (roll <= 0f)
                return candidate;
        }

        return patterns[patterns.Length - 1];
    }

    private static bool TryGetPattern(string name, out TripPattern pattern)
    {
        foreach (TripPattern candidate in Patterns)
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                pattern = candidate;

                return true;
            }
        }

        pattern = null;

        return false;
    }

    /// <summary>
    /// 発狂中ずっと掛かり続ける負荷エフェクトです。
    /// フェーズ変更時と定期リフレッシュで貼り直します。
    /// </summary>
    private static void ApplySustainedEffects(Player player, float remaining)
    {
        if (!ApplyBaseEffects) return;

        player.EnableEffect<Invigorated>(255, remaining);
        player.EnableEffect<Concussed>(255, remaining);
        player.EnableEffect<Blurred>(255, remaining);
        player.EnableEffect<AmnesiaVision>(255, remaining);
        player.EnableEffect<Asphyxiated>(5, remaining);

        // BecomingFlamingo は IHolidayEffect なので、Christmas/AprilFools が強制有効な
        // サーバーか Development/Nightly ビルドでしか登録されない。
        // 通常ビルドでのフラミンゴ表現は FogType.BecomingFlamingo 側で行う。
        player.EnableEffect<BecomingFlamingo>(1, remaining);
    }

    private static void RemoveEffects(Player player)
    {
        // 死亡後・役職変更後でも解除する必要があるので IsValid では絞らない。
        if (player?.ReferenceHub == null) return;

        player.DisableEffect<Invigorated>();
        player.DisableEffect<Concussed>();
        player.DisableEffect<Blurred>();
        player.DisableEffect<AmnesiaVision>();
        player.DisableEffect<Asphyxiated>();
        player.DisableEffect<BecomingFlamingo>();
        player.DisableEffect<FogControl>();
        player.DisableEffect<VisualTraumatized>();
        player.DisableEffect<VisualSinkhole>();
        player.DisableEffect<Deafened>();
        player.DisableEffect<Blindness>();
        player.DisableEffect<SoundtrackMute>();
        player.DisableEffect<Scp1576>();
        player.DisableEffect<AmnesiaItems>();
    }

    /// <summary>
    /// フェーズ突入時と定期リフレッシュで、そのフェーズが要求する状態へ揃え直します。
    /// </summary>
    private static void ApplyPhaseEffects(Player player, TripPhase phase, float remaining, bool onEnter)
    {
        if (player.ReferenceHub == null) return;

        ApplySustainedEffects(player, remaining);

        player.EnableEffect<FogControl>(phase.FogIntensity, remaining);

        // VisualTraumatized / VisualSinkhole は視覚だけを流用する自前エフェクト。
        // 本家の SCP-106 kill 判定や移動デバフは Patches 側で打ち消す。
        if (phase.TraumatizedIntensity > 0)
            player.EnableEffect<VisualTraumatized>(phase.TraumatizedIntensity, remaining);
        else if (onEnter)
            player.DisableEffect<VisualTraumatized>();

        if (phase.SinkholeIntensity > 0)
            player.EnableEffect<VisualSinkhole>(phase.SinkholeIntensity, remaining);
        else if (onEnter)
            player.DisableEffect<VisualSinkhole>();

        if (phase.Deafen)
            player.EnableEffect<Deafened>(255, remaining);

        if (!onEnter) return;

        if (phase.FlashOnEnter)
            player.EnableEffect<Flashed>(1, 0.7f);

        if (phase.BlindOnEnter)
            player.EnableEffect<Blindness>(255, 1.3f);
    }

    /// <summary>
    /// 毎 tick、確率で短命の視覚 / 聴覚エフェクトと霧種別を差し込みます。
    /// フェーズが変わらない間もエフェクトの切り替わりを止めないための仕掛けで、
    /// ここで足したものは <see cref="SustainRefreshSeconds"/> ごとのリフレッシュで元へ戻ります。
    /// </summary>
    private static void TickEffectFlicker(Player player, TripPhase phase, float severity, Random rng)
    {
        if (player.ReferenceHub == null) return;

        if (phase.FogFlickerChance > 0f && rng.NextDouble() < phase.FogFlickerChance * severity)
        {
            // Intensity 1 は FogType.None なので、必ず見た目が変わる 2 以上から引く。
            player.EnableEffect<FogControl>((byte)rng.Next(2, FogTypeCount + 1), 0f);
        }

        if (phase.FlickerChance <= 0f || rng.NextDouble() >= phase.FlickerChance * severity) return;

        float Roll(float min, float max) => min + (float)rng.NextDouble() * (max - min);

        switch (rng.Next(9))
        {
            case 0:
                player.EnableEffect<Flashed>(1, Roll(0.18f, 0.6f));
                break;
            case 1:
                player.EnableEffect<Blindness>(255, Roll(0.22f, 0.7f));
                break;
            case 2:
                player.EnableEffect<Deafened>(255, Roll(0.6f, 2.2f));
                break;
            case 3:
                player.EnableEffect<SoundtrackMute>(1, Roll(2f, 6f));
                break;
            case 4:
                // SCP-1576 の「遠くから声が聞こえる」歪み。
                player.EnableEffect<Scp1576>(1, Roll(1.5f, 4f));
                break;
            case 5:
                // 手に持っている物が見えなくなる。
                player.EnableEffect<AmnesiaItems>(1, Roll(0.8f, 2.4f));
                break;
            case 6:
                player.EnableEffect<VisualSinkhole>(255, Roll(0.4f, 1.4f));
                break;
            case 7:
                player.EnableEffect<VisualTraumatized>(255, Roll(0.5f, 1.8f));
                break;
            default:
                player.EnableEffect<Concussed>(255, Roll(1f, 3f));
                break;
        }
    }

    private static string NoiseHintId(Player player) => $"{player.ReferenceHub.netId}_Insanity_Noise";

    private static string MessageHintId(Player player) => $"{player.ReferenceHub.netId}_Insanity_Message";

    private static void RemoveHints(Player player)
    {
        if (player?.ReferenceHub == null) return;

        PlayerDisplay display = PlayerDisplay.Get(player);

        if (display is null) return;

        RemoveHint(display, NoiseHintId(player));
        RemoveHint(display, MessageHintId(player));

        // MaxRoamers を実行中に減らされても取りこぼさないよう、余分に舐める。
        int count = Mathf.Max(MaxRoamers, 32);

        for (int i = 0; i < count; i++)
        {
            RemoveHint(display, RoamerHintId(player, i));
        }
    }

    private static void RemoveHint(PlayerDisplay display, string id)
    {
        if (display.GetHint(id) is { } hint)
            display.RemoveHint(hint);
    }
}
