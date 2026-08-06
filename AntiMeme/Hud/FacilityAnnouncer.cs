using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Hud;

/// <summary>
/// C.A.S.S.I.E の読み上げです。
///
/// LabApi の <c>Cassie</c> ラッパーはクラスごと
/// <c>[Obsolete(error: true)]</c> になっていて参照するとコンパイルエラーになります。
/// 現行の入口は <see cref="Announcer"/> です。ここはその薄い包みで、
/// 「読み上げ文」と「字幕」を必ず対で書かせるためだけに挟んでいます。
///
/// 部隊到着や陣営検出のような文面そのものは、その部隊を持つドメイン
/// (チーム / スポーン) 側に置いてください。ここには置きません。
/// </summary>
public static class FacilityAnnouncer
{
    /// <summary>
    /// 読み上げと字幕を出します。
    /// </summary>
    /// <param name="cassie">読み上げ文。C.A.S.S.I.E が発音できる単語だけで書きます。</param>
    /// <param name="subtitle">字幕。rich text と <c>&lt;split&gt;</c> が使えます。</param>
    /// <param name="noise">ノイズ (背景音) を鳴らすか。</param>
    /// <param name="priority">読み上げ待ち行列での優先度。大きいほど先に出ます。</param>
    public static void Say(string cassie, string subtitle, bool noise = true, float priority = 0f) =>
        Announcer.Message(cassie, subtitle, noise, priority);

    /// <summary>
    /// 読み上げ待ち行列を空にします。
    /// </summary>
    public static void Clear() => Announcer.Clear();

    /// <summary>
    /// 読み上げ中かどうか。
    /// </summary>
    public static bool IsSpeaking => Announcer.IsSpeaking;

    /// <summary>
    /// 終了放送を出します。<paramref name="cause"/> が対象名の直後に連結されます。
    /// </summary>
    public static void Terminate(string targetCassie, string targetSubtitle, TerminationCause cause, bool clear = false)
    {
        if (clear)
            Clear();

        Say($"{targetCassie} {cause.Cassie}", $"{targetSubtitle} {cause.Subtitle}");
    }
}

/// <summary>
/// 終了放送の「〜は、◯◯によって終了されました。」の◯◯以降です。
/// 対象名の直後に連結される前提の文面を持ちます。
/// </summary>
public readonly struct TerminationCause
{
    /// <summary>読み上げ用。</summary>
    public readonly string Cassie;

    /// <summary>字幕用。</summary>
    public readonly string Subtitle;

    public TerminationCause(string cassie, string subtitle)
    {
        Cassie = cassie;
        Subtitle = subtitle;
    }

    /// <summary>
    /// 陣営による正常な収容として放送します。
    /// <paramref name="cassieName"/> は C.A.S.S.I.E が発音できる英字表記を渡してください
    /// (<see cref="CustomTeam"/> は表示名と色しか持っていません)。
    /// </summary>
    public static TerminationCause ByTeam(CustomTeam team, string cassieName) => new TerminationCause(
        $"contained successfully by {cassieName}",
        $"は、<color={team.Color}>{team.Name}</color>によって正常に収容されました。");

    /// <summary>H.I.D タレットによる終了。</summary>
    public static TerminationCause Hid() => new TerminationCause(
        "successfully terminated by H I D System .",
        "は、<color=yellow>H.I.D Turret</color>によって終了されました。");

    /// <summary>アンチミームプロトコルによる無効化。</summary>
    public static TerminationCause AntiMeme() => new TerminationCause(
        "Successfully neutralized by $pitch_.85 Anti- $pitch_1 Me mu Protocol.",
        "は、<color=#ff0090>アンチミームプロトコル</color>により正常に無効化されました。");

    /// <summary>原因不明の終了。</summary>
    public static TerminationCause Unknown() => new TerminationCause(
        "successfully terminated. Termination cause unspecified.",
        "は、不明な原因によって終了されました。");
}
