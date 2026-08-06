using System;
using System.Collections.Generic;
using System.Text;
using AntiMeme.Maps.Features;
using HintServiceMeow.Core.Enum;
using HintServiceMeow.Core.Extension;
using HintServiceMeow.Core.Utilities;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

using Hint = HintServiceMeow.Core.Models.Hints.Hint;
using Random = System.Random;

namespace AntiMeme.Effects;

/// <summary>
/// 発狂状態の進行そのものです。フェーズを進めながら 3 層を毎 tick 描き直します。
/// </summary>
public partial class Insanity
{
    /// <summary>
    /// メッセージ層のフォントサイズ上限です。バースト時の 1.9 倍が乗っても
    /// 一文字が画面を突き抜けない範囲に収めます。
    /// </summary>
    public static int MaxMessageFontSize { get; set; } = 210;

    private IEnumerator<float> TripCoroutine(Player player)
    {
        // Duration は ForceIntensity → Enabled() の後に入るので、1 フレーム待ってから読む。
        yield return Timing.WaitForOneFrame;

        if (!IsValid(player) || !IsEnabled) yield break;

        PlayerDisplay display = PlayerDisplay.Get(player);

        if (display is null) yield break;

        RemoveHints(player);

        Hint noise = new()
        {
            Id = NoiseHintId(player),
            Alignment = HintAlignment.Center,
            YCoordinateAlign = HintVerticalAlign.Middle,
            XCoordinate = 0f,
            YCoordinate = NoiseCenterY,
            FontSize = 26,
            Text = string.Empty,
            SyncSpeed = HintSyncSpeed.Fastest,
            BlocksDynamicHints = false,
        };

        Hint message = new()
        {
            Id = MessageHintId(player),
            Alignment = HintAlignment.Center,
            YCoordinateAlign = HintVerticalAlign.Middle,
            XCoordinate = 0f,
            YCoordinate = MessageBaseY,
            FontSize = 30,
            Text = string.Empty,
            SyncSpeed = HintSyncSpeed.Fastest,
            BlocksDynamicHints = false,
        };

        display.AddHint(noise);
        display.AddHint(message);

        Random rng = new Random(unchecked(player.PlayerId * 7919 + Environment.TickCount));
        NoiseCanvas canvas = new NoiseCanvas(GetFragments(), rng);
        RoamerField roamers = new RoamerField(display, player, rng);
        MessageContext context = new MessageContext(player);
        StringBuilder messageBuilder = new StringBuilder(512);

        float elapsed = 0f;
        int phaseIndex = -1;
        float messageTimer = 0f;
        float sustainTimer = 0f;
        float scroll = 0f;
        string currentMessage = string.Empty;
        int currentMessageFontSize = 30;
        string[] overrideBank = null;
        float overrideTimer = 0f;

        while (IsEnabled)
        {
            if (!IsValid(player)) break;

            // 服用による悪化。一度「治ったように見せて」から、より速く再発させる。
            if (pendingOverdose is { } consumed)
            {
                pendingOverdose = null;
                OverdoseProfile profile = ResolveOverdoseProfile(consumed);

                noise.Hide = true;
                message.Hide = true;
                roamers.HideAll();
                canvas.Clear();
                RemoveEffects(player);

                yield return Timing.WaitForSeconds(profile.CalmSeconds);

                if (!IsValid(player) || !IsEnabled) break;

                ApplyOverdose(profile);

                overrideBank = profile.Messages.Length > 0 ? profile.Messages : null;
                overrideTimer = profile.MessageSeconds;

                elapsed = 0f;
                phaseIndex = -1;
                messageTimer = 0f;
                sustainTimer = 0f;

                continue;
            }

            float severity = Severity;
            TripPattern current = pattern ??= PickPattern();
            float progress = ResolveProgress(current, elapsed);

            int nextPhaseIndex = ResolvePhaseIndex(current, progress);
            TripPhase phase = current.Phases[nextPhaseIndex];
            float interval = Mathf.Max(MinInterval, phase.Interval * current.SpeedScale * speedScale);

            if (nextPhaseIndex != phaseIndex)
            {
                phaseIndex = nextPhaseIndex;
                ApplyPhaseEffects(player, phase, ResolveRemaining(interval), onEnter: true);
                sustainTimer = SustainRefreshSeconds;
                messageTimer = 0f;

                if (phase.ClearNoise)
                    canvas.Clear();
            }
            else
            {
                sustainTimer -= interval;

                if (sustainTimer <= 0f)
                {
                    ApplyPhaseEffects(player, phase, ResolveRemaining(interval), onEnter: false);
                    sustainTimer = SustainRefreshSeconds;
                }
            }

            TickEffectFlicker(player, phase, severity, rng);

            BurstKind? burst = rng.NextDouble() < phase.BurstChance * severity ? PickBurst(phase, rng) : null;

            // --- ノイズ層 ---
            scroll += phase.ScrollSpeed * interval;

            if (phase.ClearNoise && burst is null)
            {
                noise.Hide = true;
            }
            else
            {
                // 経過とともに行数の目標値が加速的に増える (後半ほど一気に埋まる)。
                float fill = Mathf.Clamp01(progress / 0.8f);
                float maxLines = Mathf.Max(6f, MaxNoiseLines * severity * phase.FillRatio);
                int targetLines = Mathf.RoundToInt(Mathf.Lerp(3f, maxLines, fill * fill));

                int fontSize = ResolveNoiseFontSize(Mathf.Max(canvas.LineCount, targetLines));

                NoiseStyle style = new NoiseStyle(
                    ResolveCharsPerLine(fontSize, phase.LineWidthScale),
                    phase.ColorRunLength,
                    phase.CorruptChance * severity,
                    phase.DimNoise);

                canvas.GrowTo(targetLines, GrowPerTick, style);
                canvas.Churn(phase.ChurnLines, style);

                NoiseFrame frame = new NoiseFrame(phase.Layout, scroll, phase.NoiseSpread, phase.Shake * severity);
                ApplyNoiseBurst(noise, canvas, burst, fontSize, style, frame, phase, severity, rng);
            }

            // --- 暴走テキスト層 ---
            if (overrideTimer > 0f)
                overrideTimer -= interval;

            string[] activeOverride = overrideTimer > 0f ? overrideBank : null;

            roamers.Tick(phase, severity, interval, burst is BurstKind.Swarm, context, activeOverride);

            // --- メッセージ層 ---
            messageTimer -= interval;

            string[] bank = activeOverride ?? phase.Messages;

            if (burst is not null and not BurstKind.Blackout)
            {
                currentMessage = BuildMessage(phase, BurstMessages, rng, context, burst: true, severity, messageBuilder);
                currentMessageFontSize = ClampMessageFontSize(Mathf.RoundToInt(phase.MessageFontSize * 1.9f));
                messageTimer = phase.MessageInterval;
            }
            else if (messageTimer <= 0f)
            {
                currentMessage = BuildMessage(phase, bank, rng, context, burst: false, severity, messageBuilder);
                currentMessageFontSize = ClampMessageFontSize(phase.MessageFontSize + rng.Next(-8, 17));
                messageTimer = phase.MessageInterval;
            }

            if (burst is BurstKind.Blackout || currentMessage.Length == 0)
            {
                message.Hide = true;
            }
            else
            {
                float wander = phase.MessageWander * severity;

                message.Hide = false;
                message.FontSize = currentMessageFontSize;
                message.Text = currentMessage;
                message.XCoordinate = Jitter(rng, phase.Shake * severity * 1.4f + wander);
                message.YCoordinate = Mathf.Clamp(
                    MessageBaseY + Jitter(rng, phase.Shake * severity * 1.4f + wander * 0.9f),
                    RoamTop,
                    RoamBottom);
            }

            elapsed += interval;

            yield return Timing.WaitForSeconds(interval);
        }

        RemoveHints(player);
        tripHandle = default;
    }

    /// <summary>
    /// 服用ぶんの悪化を適用します。進行を最初から走らせ直すので、
    /// 「治ったと思ったらもう一度、しかも速く始まる」形になります。
    /// </summary>
    private void ApplyOverdose(OverdoseProfile profile)
    {
        OverdoseLevel = Mathf.Min(OverdoseLevel + 1, Mathf.Max(1, MaxOverdoseLevel));
        speedScale = Mathf.Max(0.3f, speedScale * profile.SpeedMultiplier);

        if (!string.IsNullOrEmpty(profile.PatternName) &&
            TryGetPattern(profile.PatternName, out TripPattern replacement))
        {
            pattern = replacement;
        }

        ForceIntensity(255);

        if (Duration > 0f)
        {
            // Duration と TimeLeft の両方が新しい値になるので、進捗は 0 に戻る。
            ServerChangeDuration(Mathf.Max(TimeLeft, 0f) + profile.ExtraDuration);
        }
    }

    private static OverdoseProfile ResolveOverdoseProfile(ItemType item) =>
        OverdoseProfiles.TryGetValue(item, out OverdoseProfile profile) ? profile : DefaultOverdoseProfile;

    /// <summary>
    /// 段階進行に使う 0..1 の進捗です。時間指定ありなら残り時間から、
    /// 永続付与なら <see cref="PermanentCycleSeconds"/> で周回させます。
    /// <see cref="TripPattern.Cycles"/> が 2 以上ならフェーズ列を複数回まわします。
    /// </summary>
    private float ResolveProgress(TripPattern current, float elapsed)
    {
        float duration = Duration;
        int cycles = Mathf.Max(1, current.Cycles);

        if (duration > 0f)
        {
            float progress = Mathf.Clamp01(1f - (TimeLeft / duration));

            return cycles > 1 ? Mathf.Repeat(progress * cycles, 1f) : progress;
        }

        float cycleSeconds = Mathf.Max(1f, PermanentCycleSeconds) / cycles;

        return Mathf.Repeat(elapsed, cycleSeconds) / cycleSeconds;
    }

    /// <summary>
    /// 進捗に対応するフェーズ番号です。<see cref="TripPhase.StartRatio"/> の降順で最初に該当したもの。
    /// </summary>
    private static int ResolvePhaseIndex(TripPattern current, float progress)
    {
        TripPhase[] phases = current.Phases;

        for (int i = phases.Length - 1; i >= 0; i--)
        {
            if (progress >= phases[i].StartRatio)
                return i;
        }

        return 0;
    }

    /// <summary>付随エフェクトに渡す残り時間です。永続付与なら 0 (無期限、解除時にまとめて落とす)。</summary>
    private float ResolveRemaining(float minimum) => Duration <= 0f ? 0f : Mathf.Max(TimeLeft, minimum);

    /// <summary>
    /// バースト種別に応じてノイズ層の見た目を 1 tick だけ差し替えます。
    /// </summary>
    private static void ApplyNoiseBurst(
        Hint noise,
        NoiseCanvas canvas,
        BurstKind? burst,
        int fontSize,
        in NoiseStyle style,
        in NoiseFrame frame,
        TripPhase phase,
        float severity,
        Random rng)
    {
        noise.XCoordinate = frame.Layout == NoiseLayout.Block ? Jitter(rng, frame.Shake) : 0f;
        noise.YCoordinate = NoiseCenterY + Jitter(rng, frame.Shake);

        switch (burst)
        {
            // 一部の行だけを巨大化して抜き出す。「急に文字がでかくなる」用。
            case BurstKind.Zoom:
            {
                int count = Mathf.Clamp(canvas.LineCount / 5, 2, 8);
                int start = rng.Next(0, Mathf.Max(1, canvas.LineCount - count + 1));

                noise.Hide = false;
                noise.XCoordinate = 0f;
                noise.FontSize = Mathf.RoundToInt(fontSize * 2.8f);
                noise.Text = canvas.RenderSlice(start, count, MaxNoiseChars, NoiseFrame.Plain);

                return;
            }

            // 極小フォントで画面いっぱいに一瞬だけ叩き込む。
            case BurstKind.Flood:
            {
                NoiseStyle floodStyle = new NoiseStyle(
                    84,
                    phase.ColorRunLength + 12,
                    (phase.CorruptChance + 0.2f) * severity,
                    false);

                noise.Hide = false;
                noise.XCoordinate = 0f;
                noise.FontSize = 9;
                noise.Text = canvas.RenderFresh(88, floodStyle, MaxNoiseChars, NoiseFrame.Plain);

                return;
            }

            // 表示中の内容をまるごと文字化けさせる (キャッシュは壊さない)。
            case BurstKind.Corrupt:
            {
                NoiseStyle corruptStyle = new NoiseStyle(style.CharsPerLine, 10, 0.94f, false);

                noise.Hide = false;
                noise.FontSize = fontSize;
                noise.Text = canvas.RenderFresh(Mathf.Max(canvas.LineCount, 4), corruptStyle, MaxNoiseChars, frame);

                return;
            }

            // 斜めの帯が画面を横切る。行を短くして大きくずらす。
            case BurstKind.Sweep:
            {
                NoiseStyle sweepStyle = new NoiseStyle(14, 6, (phase.CorruptChance + 0.15f) * severity, false);
                NoiseFrame sweepFrame = new NoiseFrame(NoiseLayout.Diagonal, frame.Scroll * 4f, 620f, 0f, stepPerLine: 96f);

                noise.Hide = false;
                noise.XCoordinate = 0f;
                noise.FontSize = Mathf.Clamp(fontSize + 8, 18, 40);
                noise.Text = canvas.RenderFresh(20, sweepStyle, MaxNoiseChars, sweepFrame);

                return;
            }

            // ブロック文字の壁。一瞬だけ画面が塗り潰される。
            case BurstKind.Wall:
            {
                noise.Hide = false;
                noise.XCoordinate = 0f;
                noise.FontSize = 14;
                noise.Text = canvas.RenderWall(46, 62, 8, MaxNoiseChars);

                return;
            }

            // 行が左右に割れる。画面が真っ二つにズレたように見える。
            case BurstKind.Split:
            {
                NoiseFrame splitFrame = new NoiseFrame(NoiseLayout.Column, 0f, 460f, frame.Shake);

                noise.Hide = false;
                noise.XCoordinate = 0f;
                noise.FontSize = fontSize;
                noise.Text = canvas.RenderAll(MaxNoiseChars, splitFrame);

                return;
            }

            // 1 tick だけ完全に消す。次の tick で戻るので「途切れる」感じになる。
            case BurstKind.Blackout:
                noise.Hide = true;

                return;

            // Swarm は暴走テキスト層だけを暴れさせる。ノイズ層は通常描画。
            default:
                noise.Hide = false;
                noise.FontSize = fontSize;
                noise.Text = canvas.RenderAll(MaxNoiseChars, frame);

                return;
        }
    }

    private static BurstKind PickBurst(TripPhase phase, Random rng)
    {
        BurstKind[] pool = phase.BurstPool is { Length: > 0 } ? phase.BurstPool : DefaultBurstPool;

        return pool[rng.Next(pool.Length)];
    }

    private static string BuildMessage(
        TripPhase phase,
        string[] bank,
        Random rng,
        MessageContext context,
        bool burst,
        float severity,
        StringBuilder sb)
    {
        if (bank.Length == 0) return string.Empty;
        if (!burst && rng.NextDouble() < phase.MessageBlankChance) return string.Empty;

        string body = context.Resolve(bank[rng.Next(bank.Length)], rng);

        if (body.Length == 0) return string.Empty;

        MessageStyle style = burst ? MessageStyle.Plain : PickMessageStyle(phase, rng);

        body = ApplyMessageStyle(body, style, rng);

        float corruptChance = phase.MessageCorruptChance * severity * (burst ? 2.5f : 1f);

        sb.Clear();
        sb.Append("<b>");

        if (style == MessageStyle.Marked)
            sb.Append("<mark=#c1000030>");

        if (style == MessageStyle.Spaced)
            sb.Append("<cspace=1em>");

        if (style == MessageStyle.Tilted)
            sb.Append("<rotate=").Append(rng.Next(2) == 0 ? -12 : 12).Append('>');

        if (phase.MessageColor is null)
        {
            GlitchText.GlitchWriter writer = new GlitchText.GlitchWriter(sb, phase.MessageColorRunLength, corruptChance, rng);
            writer.Feed(body);
            writer.End();
        }
        else
        {
            sb.Append("<color=").Append(phase.MessageColor).Append('>');

            foreach (char c in body)
            {
                sb.Append(corruptChance > 0f && rng.NextDouble() < corruptChance
                    ? GlitchText.RandomGlyph(rng)
                    : c);
            }

            sb.Append("</color>");
        }

        if (style == MessageStyle.Tilted)
            sb.Append("</rotate>");

        if (style == MessageStyle.Spaced)
            sb.Append("</cspace>");

        if (style == MessageStyle.Marked)
            sb.Append("</mark>");

        sb.Append("</b>");

        return sb.ToString();
    }

    private static MessageStyle PickMessageStyle(TripPhase phase, Random rng)
    {
        if (rng.NextDouble() >= phase.MessageStyleChance)
            return MessageStyle.Plain;

        // 縦積み・反復は行数を増やすので、巨大フォントのフェーズでは画面外へ飛ぶ。
        int max = phase.MessageFontSize >= 90 ? 4 : 6;

        return (MessageStyle)rng.Next(1, max);
    }

    /// <summary>本文そのものを崩す装飾です。タグではなく文字列側を触ります。</summary>
    private static string ApplyMessageStyle(string body, MessageStyle style, Random rng)
    {
        switch (style)
        {
            // 1 文字ずつ改行して縦に積む。
            case MessageStyle.Vertical:
            {
                if (body.Length > 14) return body;

                StringBuilder sb = new StringBuilder(body.Length * 2);

                for (int i = 0; i < body.Length; i++)
                {
                    if (i > 0)
                        sb.Append('\n');

                    sb.Append(body[i]);
                }

                return sb.ToString();
            }

            // 同じ文言を何度も重ねる。
            case MessageStyle.Repeated:
            {
                if (body.Length > 12) return body;

                int times = rng.Next(2, 5);
                StringBuilder sb = new StringBuilder(body.Length * times + times);

                for (int i = 0; i < times; i++)
                {
                    if (i > 0)
                        sb.Append(rng.Next(2) == 0 ? '\n' : ' ');

                    sb.Append(body);
                }

                return sb.ToString();
            }

            default:
                return body;
        }
    }

    private static int ClampMessageFontSize(int fontSize) =>
        Mathf.Clamp(fontSize, 12, Mathf.Max(12, MaxMessageFontSize));

    /// <summary>行数が増えるほどフォントを縮め、画面の縦幅に収まるようにします。</summary>
    private static int ResolveNoiseFontSize(int lineCount) =>
        lineCount <= 0 ? 26 : Mathf.Clamp(Mathf.RoundToInt(NoiseAreaHeight / (lineCount * 1.25f)), 9, 26);

    /// <summary>フォントが小さいほど 1 行に詰め込む文字数を増やし、横方向も埋めます。</summary>
    private static int ResolveCharsPerLine(int fontSize, float widthScale)
    {
        int full = Mathf.Clamp(Mathf.RoundToInt(1500f / fontSize), 24, 72);

        return Mathf.Clamp(Mathf.RoundToInt(full * Mathf.Clamp(widthScale, 0.15f, 1f)), 6, 72);
    }

    /// <summary>
    /// 文書本文からリッチテキストタグを除いて <see cref="FragmentLength"/> 文字ずつに刻んだ
    /// ノイズ素材を作ります。<see cref="SystemFragments"/> も混ぜて「機械のログ」感を足します。
    /// </summary>
    private static string[] GetFragments()
    {
        if (fragments is not null) return fragments;

        List<string> collected = [];

        foreach (DocumentType type in DocumentDictionary.DefinedTypes)
        {
            string plain = StripTags(DocumentDictionary.Get(type));

            foreach (string rawLine in plain.Split('\n'))
            {
                string line = rawLine.Trim('\r', ' ', '\t');

                if (line.Length == 0) continue;

                for (int i = 0; i < line.Length; i += FragmentLength)
                {
                    collected.Add(line.Substring(i, Math.Min(FragmentLength, line.Length - i)));
                }
            }
        }

        // 文書だけだと語彙が固まるので、システムログ風の断片を厚めに混ぜる。
        int repeat = Mathf.Max(1, collected.Count / Mathf.Max(1, SystemFragments.Length * 6));

        for (int i = 0; i < repeat; i++)
        {
            collected.AddRange(SystemFragments);
        }

        // 素材が 1 つも取れないと行生成が破綻するので、最低限の受け皿を入れておく。
        if (collected.Count == 0)
            collected.Add("■■■■■■■■");

        return fragments = collected.ToArray();
    }

    /// <summary>リッチテキストタグを取り除きます。</summary>
    private static string StripTags(string source)
    {
        if (string.IsNullOrEmpty(source)) return string.Empty;

        StringBuilder sb = new StringBuilder(source.Length);
        bool inTag = false;

        foreach (char c in source)
        {
            if (c == '<')
            {
                inTag = true;

                continue;
            }

            if (c == '>')
            {
                inTag = false;

                continue;
            }

            if (!inTag)
                sb.Append(c);
        }

        return sb.ToString();
    }
}
