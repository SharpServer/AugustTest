using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Input;

/// <summary>
/// プレイヤーが押せるキー 1 つ分です。<b>入力 1 つ = クラス 1 つ</b>。
///
/// <para>
/// クラスを書くだけで <see cref="InputHandler"/> が Server Specific Settings に並べ、
/// 押されたら <see cref="OnPressed"/> を呼びます。ID の採番も定義表も要りません。
/// 旧実装は <c>SettingKey</c> enum と <c>SettingDefinition</c> のリストと
/// 受信側の巨大な switch の 3 点セットでした。
/// </para>
/// </summary>
/// <example>
/// <code>
/// public sealed class UseAbilityKey : InputBinding
/// {
///     public override string Label      => "アビリティ使用";
///     public override KeyCode DefaultKey => KeyCode.LeftAlt;
///
///     public override void OnPressed(Player player) => AbilityBar.Use(player);
/// }
/// </code>
/// </example>
public abstract class InputBinding
{
    /// <summary>
    /// 設定画面に出る名前です。
    /// </summary>
    public abstract string Label { get; }

    /// <summary>
    /// 推奨キーです。プレイヤーが変更できます。
    /// </summary>
    public abstract KeyCode DefaultKey { get; }

    /// <summary>
    /// 設定画面に出る説明です。
    /// </summary>
    public virtual string Hint => string.Empty;

    /// <summary>
    /// 設定画面での並び順です。小さいほど上。
    /// </summary>
    public virtual int Order => 100;

    /// <summary>
    /// このキーが属する見出しです。同じ文字列のものがまとまります。
    /// </summary>
    public virtual string Group => "操作";

    /// <summary>
    /// 観戦中でも反応させるか。
    /// </summary>
    public virtual bool AllowWhileSpectating => false;

    /// <summary>
    /// 押されたときの処理です。
    /// </summary>
    public abstract void OnPressed(Player player);
}
