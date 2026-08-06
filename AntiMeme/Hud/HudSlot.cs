using System;
using HintServiceMeow.Core.Enum;
using LabApi.Features.Wrappers;

namespace AntiMeme.Hud;

/// <summary>
/// 画面上の 1 区画です。「どこに・どの大きさで出すか」だけを持ち、中身は持ちません。
///
/// 中身の入れ方は 2 通りあります。どちらか一方だけを使ってください
/// (両方使うとループ側の書き込みが押し込みを上書きします)。
///
/// <list type="number">
/// <item>
/// <b>押し込み (push)</b> — 出したいときに 1 回だけ書く。
/// <c>player.Hud().Set(HudSlot.Event, "...")</c>
/// </item>
/// <item>
/// <b>引き出し (pull)</b> — 表示元の関数を 1 度だけ登録しておくと、
/// <see cref="Interval"/> ごとに全員分が更新される。
/// <c>HudSlot.Ability.Bind(subject =&gt; BuildAbilityText(subject))</c>
/// </item>
/// </list>
///
/// pull の引数は「観戦中なら観戦先」です。観戦の面倒はここで吸収するので、
/// 各ドメインは「このプレイヤーの情報をどう文字にするか」だけを書けば済みます。
/// </summary>
/// <example>
/// <code>
/// // 役職ドメイン: 自分の表示テキストを 1 行で HUD に載せる
/// HudSlot.Role.Bind(subject =&gt; subject.CustomInfo);
///
/// // 単発の通知はスロットを使わない
/// player.Notify("拾得しました", 3f);
/// </code>
/// </example>
public sealed class HudSlot
{
    /// <summary>
    /// <see cref="ServerInfo"/> の既定文言です。<c>Config.IsBeta</c> のときだけ BETA を添えます。
    /// 別のものを出したいときは <see cref="Bind(Func{Player, string})"/> で差し替えます。
    /// </summary>
    public static string DefaultServerInfo =>
        AntiMemePlugin.Settings.IsBeta
            ? "[<color=#008cff>Sharp Server</color> - <color=red>BETA</color>]"
            : "[<color=#008cff>Sharp Server</color>]";

    private HudSlot(
        string id,
        float x,
        float y,
        int fontSize,
        HintAlignment alignment,
        HintSyncSpeed syncSpeed,
        float interval,
        HintVerticalAlign verticalAlign = HintVerticalAlign.Middle)
    {
        Id = id;
        X = x;
        Y = y;
        FontSize = fontSize;
        Alignment = alignment;
        SyncSpeed = syncSpeed;
        Interval = interval;
        VerticalAlign = verticalAlign;
    }

    /// <summary>サーバー名。ロビー中も出ます。</summary>
    public static readonly HudSlot ServerInfo =
        new HudSlot("Hud.ServerInfo", 0f, 1050f, 18, HintAlignment.Center, HintSyncSpeed.UnSync, 5f);

    /// <summary>役職名。</summary>
    public static readonly HudSlot Role =
        new HudSlot("Hud.Role", -350f, 860f, 23, HintAlignment.Left, HintSyncSpeed.Fastest, 1f);

    /// <summary>陣営名。</summary>
    public static readonly HudSlot Team =
        new HudSlot("Hud.Team", -350f, 885f, 23, HintAlignment.Left, HintSyncSpeed.Fastest, 1f);

    /// <summary>目的。</summary>
    public static readonly HudSlot Objective =
        new HudSlot("Hud.Objective", -350f, 915f, 25, HintAlignment.Left, HintSyncSpeed.Fastest, 1f);

    /// <summary>進行中のゲームモード名。</summary>
    public static readonly HudSlot Event =
        new HudSlot("Hud.Event", -350f, 120f, 26, HintAlignment.Left, HintSyncSpeed.Fast, 1f);

    /// <summary>役職ごとの固有情報 (残弾・スタック・所持数など)。</summary>
    public static readonly HudSlot Specific =
        new HudSlot("Hud.Specific", 0f, 880f, 23, HintAlignment.Left, HintSyncSpeed.Fastest, 0.5f);

    /// <summary>アビリティの状態とキーバインド。</summary>
    public static readonly HudSlot Ability =
        new HudSlot("Hud.Ability", 0f, 800f, 22, HintAlignment.Left, HintSyncSpeed.Fastest, 0.5f);

    /// <summary>
    /// 掛かっている効果。右下から上へ積みます。
    /// 下端を基準にしているので、効果が増えても画面外へはみ出しません。
    /// </summary>
    public static readonly HudSlot Effects =
        new HudSlot("Hud.Effects", 370f, 980f, 20, HintAlignment.Right, HintSyncSpeed.Fastest, 0.5f,
            HintVerticalAlign.Bottom);

    /// <summary>
    /// 味方一覧 (旧 ScpStatusHints の置き場)。右上から下へ積みます。
    /// </summary>
    public static readonly HudSlot Roster =
        new HudSlot("Hud.Roster", 370f, 100f, 22, HintAlignment.Right, HintSyncSpeed.Fastest, 0.5f,
            HintVerticalAlign.Top);

    /// <summary>観戦者向けの財団 / カオス再出撃タイマー。</summary>
    public static readonly HudSlot Respawn =
        new HudSlot("Hud.Respawn", 0f, 90f, 24, HintAlignment.Center, HintSyncSpeed.Fastest, 0.5f);

    /// <summary>ボス HP バー。<see cref="BossBar"/> が自分で書き込みます。</summary>
    public static readonly HudSlot BossBar =
        new HudSlot("Hud.BossBar", 0f, 200f, 24, HintAlignment.Center, HintSyncSpeed.Fastest, 0.25f);

    /// <summary>デバッグ表示。</summary>
    public static readonly HudSlot Debug =
        new HudSlot("Hud.Debug", -350f, 345f, 24, HintAlignment.Left, HintSyncSpeed.Fast, 0.1f);

    /// <summary>
    /// 全スロット。<see cref="HudLoop"/> がここを回して pull を更新します。
    /// </summary>
    public static readonly HudSlot[] All =
    [
        ServerInfo, Role, Team, Objective, Event,
        Specific, Ability, Effects, Roster, Respawn, BossBar, Debug,
    ];

    /// <summary>HintServiceMeow 上でのヒント ID。</summary>
    public string Id { get; }

    /// <summary>横位置。</summary>
    public float X { get; }

    /// <summary>縦位置。値が大きいほど画面下。</summary>
    public float Y { get; }

    /// <summary>文字サイズ。</summary>
    public int FontSize { get; }

    /// <summary>横揃え。</summary>
    public HintAlignment Alignment { get; }

    /// <summary>
    /// <see cref="Y"/> がテキストブロックのどこを指すか。
    /// <see cref="HintVerticalAlign.Top"/> なら下へ、<see cref="HintVerticalAlign.Bottom"/> なら
    /// 上へ伸びます。行数が変わるスロットはここを決めておかないと、
    /// 中身が増えたときに隣のスロットへ食い込みます。
    /// </summary>
    public HintVerticalAlign VerticalAlign { get; }

    /// <summary>HintServiceMeow の同期速度。</summary>
    public HintSyncSpeed SyncSpeed { get; }

    /// <summary>pull の更新間隔 (秒)。0 なら push 専用でループを回しません。</summary>
    public float Interval { get; }

    /// <summary>
    /// ラウンド開始待ち (ロビー) の間も描くかどうか。
    /// 既定は false で、旧実装が <c>Round.IsLobby</c> のとき
    /// <c>PlayerHUDSetup</c> ごと打ち切っていたのと同じ見え方になります。
    /// ここを true にできるのは、役職に依存しないもの (サーバー名・デバッグ) だけです。
    /// </summary>
    public bool ShowsInLobby => ReferenceEquals(this, ServerInfo) || ReferenceEquals(this, Debug);

    /// <summary>
    /// pull の表示元。第 1 引数が画面の持ち主、第 2 引数が表示対象 (観戦中なら観戦先)。
    /// null なら push 専用として扱います。
    /// </summary>
    public Func<Player, Player, HudContent> Source { get; private set; }

    /// <summary>
    /// 表示対象だけを見る表示元を登録します。ほとんどの用途はこちらです。
    /// </summary>
    public void Bind(Func<Player, string> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        Source = (_, subject) => source(subject);
    }

    /// <summary>
    /// 画面の持ち主と表示対象の両方を見る表示元を登録します。
    /// 「誰に見せるか」で内容が変わるもの (味方一覧・ボスバー) で使います。
    /// </summary>
    public void Bind(Func<Player, Player, string> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        Source = (viewer, subject) => source(viewer, subject);
    }

    /// <summary>
    /// キーバインドなど、クライアント側で解決させたい差し込み値を含む表示元を登録します。
    /// </summary>
    public void Bind(Func<Player, HudContent> source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        Source = (_, subject) => source(subject);
    }

    /// <summary>
    /// pull の登録を外します。以降は push 専用になります。
    /// </summary>
    public void Unbind() => Source = null;

    static HudSlot()
    {
        ServerInfo.Bind(_ => DefaultServerInfo);
    }
}
