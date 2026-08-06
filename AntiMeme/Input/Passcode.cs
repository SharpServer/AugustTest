using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Input;

/// <summary>
/// 扉や端末で使う番号を控えておく欄です。
/// 特殊扉 (<c>Maps.Core.SpecialDoor</c>) が「番号を知っていること」の判定に読みます。
/// </summary>
/// <remarks>
/// サーバー側では番号を検証しません。扉の側が自分の番号と比べます。
/// </remarks>
public sealed class Passcode : TextSetting
{
    /// <inheritdoc/>
    public override string Label => "パスコード";

    /// <inheritdoc/>
    public override string Hint =>
        "扉や端末で使う番号を控えておく欄です。入力した番号は他プレイヤーには見えません。";

    /// <inheritdoc/>
    public override string Placeholder => "----";

    /// <inheritdoc/>
    public override int MaxLength => 16;

    /// <inheritdoc/>
    public override int Order => 10;

    /// <summary>今このプレイヤーが控えている番号です。</summary>
    public static string Current(Player player) => InputHandler.Setting<Passcode>()?.Of(player) ?? string.Empty;

    /// <summary>控えている番号が <paramref name="expected"/> と一致するか。</summary>
    public static bool Has(Player player, string expected) =>
        InputHandler.Setting<Passcode>() is { } setting && setting.Matches(player, expected);
}
