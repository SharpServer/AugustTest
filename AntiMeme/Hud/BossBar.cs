using System;
using System.Collections.Generic;
using System.Text;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Hud;

/// <summary>
/// ボス HP バーの表示部品です。<see cref="HudSlot.BossBar"/> に描かれます。
///
/// 旧実装は Broadcast を 1 秒ごとに撃ち直して「常駐するバー」を偽装していたため、
/// 他のブロードキャスト (勝利演出・アナウンス) と潰し合っていました。
/// HintServiceMeow のヒントに載せれば撃ち直しが要らず、潰し合いも起きません。
///
/// 複数のバーを同時に出せます。<see cref="Order"/> が小さいものから上に並び、
/// 同値なら <see cref="Show"/> した順です。3 層描画や幕進行はこの上に載せてください。
/// </summary>
/// <example>
/// <code>
/// BossBar bar = new BossBar { Title = "DANTE", BarColor = "#39ff14", MaxValue = 5000f };
/// bar.Show();
/// bar.Value = currentHp;
/// bar.Subtitle = "第二幕";
/// bar.StateText = "★ 無敵 ★";   // 設定するとバー行の代わりに出る
/// bar.Hide();
///
/// // プレイヤーの HP をそのまま出す
/// new BossBar { Title = "SERGEY" }.Track(player).Show();
/// </code>
/// </example>
public sealed class BossBar
{
    private static readonly List<BossBar> Shown = new List<BossBar>();

    private static RoundScope armedScope;
    private static long nextSequence;

    private long sequence;

    /// <summary>見出し (ボス名)。</summary>
    public string Title { get; set; } = "BOSS";

    /// <summary>見出しの色。</summary>
    public string TitleColor { get; set; } = "#ff1a1a";

    /// <summary>第 2 見出し (幕名・フェーズ名)。空なら出しません。</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>最大値。</summary>
    public float MaxValue { get; set; } = 1f;

    /// <summary>現在値。</summary>
    public float Value { get; set; } = 1f;

    /// <summary>バーの色。</summary>
    public string BarColor { get; set; } = "#ff3333";

    /// <summary>バーの分割数。</summary>
    public int Segments { get; set; } = 20;

    /// <summary>現在値 / 最大値 を数字でも出すか。</summary>
    public bool ShowNumbers { get; set; } = true;

    /// <summary>
    /// 設定するとバー行の代わりにこの文字列を出します (無敵中など)。空でバー表示に戻ります。
    /// </summary>
    public string StateText { get; set; } = string.Empty;

    /// <summary>
    /// 追跡対象。設定すると描画のたびに Health / MaxHealth をバーへ写します。
    /// 対象が死ぬか居なくなったらバーは自動で消えます。
    /// </summary>
    public Player Tracked { get; set; }

    /// <summary>並び順。小さいほど上。</summary>
    public int Order { get; set; }

    /// <summary>見せる相手。null なら全員。</summary>
    public Func<Player, bool> Viewers { get; set; }

    /// <summary>表示中か。</summary>
    public bool IsShown { get; private set; }

    /// <summary>
    /// HSM のスロット結線を確実に行います。呼び出し自体は副作用を持たず、
    /// 静的初期化を遅延させないための入口です。
    /// </summary>
    public static void EnsureInitialized()
    {
        if (HudSlot.BossBar.Source is null)
            HudSlot.BossBar.Bind((viewer, _) => RenderFor(viewer));
    }

    /// <summary>プレイヤーの HP をそのままバーにします。</summary>
    public BossBar Track(Player player)
    {
        Tracked = player;

        return this;
    }

    /// <summary>表示を始めます。</summary>
    public BossBar Show()
    {
        if (IsShown) return this;

        IsShown = true;
        sequence = ++nextSequence;

        int index = Shown.Count;

        while (index > 0 && Shown[index - 1].Order > Order)
        {
            index--;
        }

        Shown.Insert(index, this);
        Arm();

        return this;
    }

    /// <summary>表示を止めます。</summary>
    public void Hide()
    {
        if (!IsShown) return;

        IsShown = false;
        Shown.Remove(this);
    }

    /// <summary>表示中のバーをすべて止めます。</summary>
    public static void HideAll()
    {
        foreach (BossBar bar in Shown.ToArray())
        {
            bar.Hide();
        }
    }

    /// <summary>このバー 1 本ぶんの文字列。</summary>
    public string Render()
    {
        StringBuilder builder = new StringBuilder();

        builder.Append("<size=24><b><color=").Append(TitleColor).Append('>')
            .Append(Title).Append("</color></b>");

        if (!string.IsNullOrEmpty(Subtitle))
            builder.Append("  <color=#bbbbbb>─ ").Append(Subtitle).Append(" ─</color>");

        builder.Append("</size>\n");

        if (!string.IsNullOrEmpty(StateText))
        {
            builder.Append(StateText);

            return builder.ToString();
        }

        float value = Mathf.Max(0f, Value);

        builder.Append("<size=26><color=").Append(BarColor).Append('>')
            .Append(RenderBar(value, MaxValue, Segments))
            .Append("</color></size>");

        if (ShowNumbers)
        {
            builder.Append("  <size=18>")
                .Append(Mathf.CeilToInt(value)).Append(" / ").Append(Mathf.CeilToInt(MaxValue))
                .Append("</size>");
        }

        return builder.ToString();
    }

    /// <summary>値と最大値から █ / ░ の帯だけを作ります。</summary>
    public static string RenderBar(float value, float max, int segments = 20)
    {
        segments = Math.Max(1, segments);

        float ratio = max > 0f ? Mathf.Clamp01(value / max) : 0f;
        int filled = Mathf.Clamp(Mathf.RoundToInt(ratio * segments), 0, segments);

        return new string('█', filled) + new string('░', segments - filled);
    }

    /// <summary>
    /// この観戦者に見えるバーをすべて縦に並べた文字列。表示するものが無ければ空文字。
    /// </summary>
    private static string RenderFor(Player viewer)
    {
        if (Shown.Count == 0) return string.Empty;

        StringBuilder builder = new StringBuilder();

        foreach (BossBar bar in Shown.ToArray())
        {
            if (!bar.SyncTracked()) continue;
            if (bar.Viewers is not null && !bar.Viewers(viewer)) continue;

            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append(bar.Render());
        }

        return builder.ToString();
    }

    /// <summary>
    /// 追跡対象の HP をバーへ写します。対象が無効なら自分を消して false を返します。
    /// </summary>
    private bool SyncTracked()
    {
        if (Tracked is null) return true;

        if (Tracked is not { IsDestroyed: false, IsAlive: true })
        {
            Hide();

            return false;
        }

        MaxValue = Mathf.Max(1f, Tracked.MaxHealth);
        Value = Mathf.Clamp(Tracked.Health, 0f, MaxValue);

        return true;
    }

    private static void Arm()
    {
        RoundScope scope = RoundScope.Current;

        if (ReferenceEquals(armedScope, scope)) return;

        armedScope = scope;
        scope.OnEnd(HideAll);
    }

}
