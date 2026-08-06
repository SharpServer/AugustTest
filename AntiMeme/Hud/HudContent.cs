using HintParameter = Hints.HintParameter;

namespace AntiMeme.Hud;

/// <summary>
/// スロットに書き込む中身です。文字列と、ゲーム側に解決させる差し込み値
/// (<see cref="HintParameter"/>) の組です。
/// </summary>
/// <remarks>
/// キーバインドを画面に出すには、サーバーが文字を書くのではなく
/// <c>SSKeybindHintParameter</c> をクライアントに解決させる必要があります
/// (割り当てキーはプレイヤーごとに違うため)。文字列だけで済む大半のスロットのために
/// <see cref="string"/> からの暗黙変換を用意してあるので、
/// 使わない側は今までどおり文字列を返すだけで構いません。
/// </remarks>
public readonly struct HudContent
{
    public HudContent(string text, HintParameter[] parameters = null)
    {
        Text = text ?? string.Empty;
        Parameters = parameters;
    }

    /// <summary>表示文字列です。<c>{0}</c> のような差し込み位置を含められます。</summary>
    public string Text { get; }

    /// <summary>差し込み値です。null なら差し込みなし。</summary>
    public HintParameter[] Parameters { get; }

    public static implicit operator HudContent(string text) => new HudContent(text);
}
