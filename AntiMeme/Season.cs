namespace AntiMeme;

/// <summary>
/// 季節です。装飾・キャンディの中身・戦士陣営の呼称などが切り替わります。
/// </summary>
/// <remarks>
/// 振る舞いを持たない閉じた値集合なので、型ではなく enum のままにしています。
/// 「今どれか」は運営が設定で決めるものなので <see cref="Config.Season"/> にあります。
/// </remarks>
public enum Season
{
    /// <summary>平常。</summary>
    None,

    /// <summary>エイプリルフール。</summary>
    April,

    /// <summary>第五祭。</summary>
    FifthFestival,

    /// <summary>夏。</summary>
    Summer,

    /// <summary>ハロウィン。</summary>
    Halloween,

    /// <summary>クリスマス。</summary>
    Christmas,
}
