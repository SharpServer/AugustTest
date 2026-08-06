using System;
using System.Collections.Generic;
using AntiMeme.Maps.Features;
using CustomPlayerEffects;
using HintServiceMeow.Core.Extension;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using RemoteAdmin.Interfaces;
using UnityEngine;

using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using Random = System.Random;

namespace AntiMeme.Effects;

/// <summary>
/// 発狂状態。付与時に進行パターン (<see cref="TripPattern"/>) を 1 つ抽選し、
/// そのフェーズ列を効果時間をかけて進んでいく幻覚エフェクトです。
///
/// <para>
/// 画面は 3 層です。実際の描画は <c>Insanity.Layers.cs</c>、
/// フェーズ定義と文言バンクは <c>Insanity.Content.cs</c> にあります。
/// ここはそれらを駆動する本体です。
/// </para>
/// <list type="bullet">
/// <item>ノイズ層 — 行を溜め込み、行数が増えるほどフォントを縮めて画面を埋める</item>
/// <item>メッセージ層 — 巨大な一文。位置・装飾・文字化けがフェーズごとに変わる</item>
/// <item>暴走テキスト層 — 独立した Hint が速度ベクトルを持って画面内を走り、壁で跳ね返る</item>
/// </list>
/// <para>
/// 鎮痛剤 / アドレナリン / SCP-500 / メディキットを飲むと
/// <see cref="ICustomHealableEffect.OnHeal"/> 経由で「一瞬治ったように見せてから、
/// より速く・より悪い進行パターンで再発する」。飲むほど <see cref="OverdoseLevel"/> が上がります。
/// </para>
/// <para>
/// <see cref="StatusEffectBase.Intensity"/> は発狂の強さです (255 = 全開)。
/// 画面揺れ・バースト率・文字化け率・ノイズ行数・暴走テキスト数がこの比率でスケールします。
/// 段階の進行そのものは残り時間で決まるので Intensity では変わりません。
/// </para>
/// </summary>
public partial class Insanity : CustomEffect, ICustomDisplayName, ICustomHealableEffect
{
    // ===== 調整できるパラメータ =====

    /// <summary>Duration を指定せずに付与したとき (永続) の 1 サイクル秒数。</summary>
    public static float PermanentCycleSeconds { get; set; } = 60f;

    /// <summary>ノイズ層が最終的に到達する行数 (Intensity 255 のとき)。</summary>
    public static int MaxNoiseLines { get; set; } = 56;

    /// <summary>1 tick で追加できる行数の上限。</summary>
    public static int GrowPerTick { get; set; } = 5;

    /// <summary>ノイズ層が使える縦幅の目安 (HintServiceMeow 座標)。</summary>
    public static float NoiseAreaHeight { get; set; } = 980f;

    /// <summary>Hint 1 枚あたりのタグ込み最大文字数。暴走時の保険。</summary>
    public static int MaxNoiseChars { get; set; } = 12000;

    /// <summary>暴走テキスト層が確保する Hint の数 (= 同時表示の上限)。</summary>
    public static int MaxRoamers { get; set; } = 14;

    /// <summary>暴走テキストが跳ね返る左右の壁 (中央からの距離)。</summary>
    public static float RoamHalfWidth { get; set; } = 520f;

    /// <summary>暴走テキストが跳ね返る上の壁。</summary>
    public static float RoamTop { get; set; } = 90f;

    /// <summary>暴走テキストが跳ね返る下の壁。</summary>
    public static float RoamBottom { get; set; } = 1010f;

    /// <summary>発狂中プレイヤーを SCP-513 の付きまとい対象にするか。</summary>
    public static bool StalkWithScp513 { get; set; } = true;

    /// <summary>ベースで掛け続ける負荷エフェクトを使うか。</summary>
    public static bool ApplyBaseEffects { get; set; } = true;

    /// <summary>医療アイテム服用で悪化させるか。false にすると服用は完全に無視されます (治りもしません)。</summary>
    public static bool AggravateOnMedicalItem { get; set; } = true;

    /// <summary>服用による悪化の上限回数。</summary>
    public static int MaxOverdoseLevel { get; set; } = 5;

    /// <summary>tick 間隔の下限。速度倍率が掛かってもこれより速くはなりません。</summary>
    public static float MinInterval { get; set; } = 0.08f;

    /// <summary>
    /// 抽選せずに必ずこのパターンを使います。null なら <see cref="Patterns"/> から重み付き抽選。
    /// デバッグ・イベント用。
    /// </summary>
    public static string ForcedPatternName { get; set; }

    // ===== レイアウト定数 =====

    /// <summary>元テキストを刻む単位。行はこの断片を繋いで作ります。</summary>
    private const int FragmentLength = 24;

    private const float NoiseCenterY = 545f;
    private const float MessageBaseY = 520f;

    /// <summary>付随エフェクトを貼り直す間隔 (秒)。フリッカーや医療アイテムで剥がれた分をここで戻します。</summary>
    private const float SustainRefreshSeconds = 1.4f;

    /// <summary>文書本文をタグ除去・断片化してキャッシュしたノイズ素材。初回使用時に 1 度だけ作ります。</summary>
    private static string[] fragments;

    private CoroutineHandle tripHandle;
    private TripPattern pattern;
    private float speedScale = 1f;
    private ItemType? pendingOverdose;

    /// <inheritdoc/>
    public bool CanBeDisplayed => true;

    /// <inheritdoc/>
    public string DisplayName => "発狂状態";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Negative;

    /// <summary>これまでに医療アイテムを飲んで悪化した回数。</summary>
    public int OverdoseLevel { get; private set; }

    /// <summary>現在走っている進行パターン名。未開始なら空文字。</summary>
    public string PatternName => pattern?.Name ?? string.Empty;

    /// <summary>Intensity を 0..1 に正規化した「発狂の強さ」。</summary>
    private float Severity => Mathf.Clamp01(Intensity / 255f);

    /// <inheritdoc/>
    protected override void OnEffectEnabled()
    {
        if (!NetworkServer.active || Player is not { IsDestroyed: false }) return;

        pattern = PickPattern();
        speedScale = 1f;
        OverdoseLevel = 0;
        pendingOverdose = null;

        if (StalkWithScp513)
            Scp513.AddTarget(Player);

        StopTrip();
        tripHandle = Timing.RunCoroutine(TripCoroutine(Player));
    }

    /// <inheritdoc/>
    protected override void OnEffectDisabled() => Cleanup(Player);

    /// <inheritdoc/>
    public override void OnDestroy()
    {
        Cleanup(Player);
        base.OnDestroy();
    }

    /// <summary>
    /// この 4 種を「発狂状態に効く薬」として名乗り出ることで、
    /// ゲーム本体の医療アイテム処理から <see cref="OnHeal"/> を呼んでもらいます。
    /// </summary>
    public bool IsHealable(ItemType item) => AggravateOnMedicalItem && OverdoseProfiles.ContainsKey(item);

    /// <summary>
    /// <see cref="ICustomHealableEffect"/> なので本家は解除処理をしません。
    /// ここでは解除する代わりに悪化を予約します。
    ///
    /// 呼び出し元は効果一覧を foreach 中なので、この場でエフェクトを触ると
    /// 同じループの後続要素を踏みます。実処理はコルーチン側の次 tick に回します。
    /// </summary>
    public void OnHeal(ItemType item)
    {
        if (!AggravateOnMedicalItem || !IsEnabled) return;

        pendingOverdose = item;
    }

    private void Cleanup(Player player)
    {
        StopTrip();

        if (!NetworkServer.active) return;

        player ??= Hub == null ? null : Player.Get(Hub);

        if (player is not { IsDestroyed: false }) return;

        if (StalkWithScp513)
            Scp513.RemoveTarget(player);

        RemoveEffects(player);
        RemoveHints(player);
    }

    private void StopTrip()
    {
        if (tripHandle.IsRunning)
            Timing.KillCoroutines(tripHandle);

        tripHandle = default;
    }

    private static float Jitter(Random rng, float amplitude) =>
        amplitude <= 0f ? 0f : (float)((rng.NextDouble() * 2d - 1d) * amplitude);

    private static string RoamerHintId(Player player, int index) =>
        $"{player.ReferenceHub.netId}_Insanity_Roam{index}";

    private static string ResolveNickname(Player player)
    {
        string nickname = StripTags(player.Nickname ?? string.Empty);

        if (nickname.Length > 16)
            nickname = nickname.Substring(0, 16);

        return string.IsNullOrWhiteSpace(nickname) ? "■■■■" : nickname;
    }

    private static bool IsValid(Player player) =>
        player is { IsDestroyed: false } && player.ReferenceHub != null;
}
