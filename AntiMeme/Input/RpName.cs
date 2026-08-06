using LabApi.Features.Wrappers;

namespace AntiMeme.Input;

/// <summary>
/// ロールプレイ用の表示名です。入力するとネームプレートが
/// 「入力名 (本来の名前)」になります。
/// </summary>
/// <remarks>
/// 旧実装は <c>RPNameSetter</c> という別ハンドラが設定値の保存・組み立て・
/// <c>CustomName</c> への反映を全部持っていました。
/// 入力欄自身が変更を受け取れるので、反映もここに書けば足ります。
/// </remarks>
public sealed class RpName : TextSetting
{
    /// <inheritdoc/>
    public override string Label => "RP ネーム";

    /// <inheritdoc/>
    public override string Hint => "入力すると、名前が「入力名 (本来の名前)」の形で表示されます。";

    /// <inheritdoc/>
    public override string Placeholder => "未設定";

    /// <inheritdoc/>
    public override int MaxLength => 24;

    /// <inheritdoc/>
    public override int Order => 20;

    /// <inheritdoc/>
    protected override void OnChanged(Player player, string value) =>
        player.DisplayName = string.IsNullOrWhiteSpace(value)
            ? player.Nickname
            : $"{value} ({player.Nickname})";
}
