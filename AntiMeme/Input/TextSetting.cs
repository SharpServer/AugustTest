using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace AntiMeme.Input;

/// <summary>
/// プレイヤーが設定画面へ打ち込む入力欄 1 つ分です。<b>入力欄 1 つ = クラス 1 つ</b>。
///
/// <para>
/// クラスを書くだけで <see cref="InputHandler"/> が Server Specific Settings に並べ、
/// 入力を保持します。値の読み方は <see cref="Of"/>。
/// </para>
/// </summary>
/// <example>
/// <code>
/// public sealed class Passcode : TextSetting
/// {
///     public override string Label => "パスコード";
///
///     protected override void OnChanged(Player player, string value) { }
/// }
/// </code>
/// </example>
public abstract class TextSetting
{
    private readonly Dictionary<Player, string> values = new Dictionary<Player, string>();

    /// <summary>設定画面に出る見出しです。</summary>
    public abstract string Label { get; }

    /// <summary>入力欄の説明です。</summary>
    public virtual string Hint => string.Empty;

    /// <summary>未入力のときに薄く出る文字です。</summary>
    public virtual string Placeholder => "...";

    /// <summary>入力できる文字数です。</summary>
    public virtual int MaxLength => 32;

    /// <summary>設定画面での並び順です。小さいほど上。</summary>
    public virtual int Order => 100;

    /// <summary>このプレイヤーが入力した値です。未入力なら空文字。</summary>
    public string Of(Player player) =>
        player is not null && values.TryGetValue(player, out string value) ? value : string.Empty;

    /// <summary>入力が <paramref name="expected"/> と一致するか。</summary>
    public bool Matches(Player player, string expected) =>
        !string.IsNullOrEmpty(expected) && Of(player) == expected;

    /// <summary>入力が変わったときに呼ばれます。</summary>
    protected virtual void OnChanged(Player player, string value)
    {
    }

    /// <summary>受信した入力を控えます。<see cref="InputHandler"/> から呼びます。</summary>
    internal void Set(Player player, string value)
    {
        if (player is null) return;

        values[player] = value?.Trim() ?? string.Empty;

        OnChanged(player, values[player]);
    }

    /// <summary>退出したプレイヤーの控えを捨てます。</summary>
    internal void Forget(Player player)
    {
        if (player is not null)
            values.Remove(player);
    }

    /// <summary>すべての控えを捨てます。</summary>
    internal void Clear() => values.Clear();
}
