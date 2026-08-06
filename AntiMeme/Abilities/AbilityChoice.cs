using LabApi.Features.Wrappers;

namespace AntiMeme.Abilities;

/// <summary>
/// 切り替え式アビリティの選択肢です。<b>選択肢そのものが効果を持ちます。</b>
///
/// <para>
/// 旧 <c>AbilityOption</c> は <c>Id</c> / <c>DisplayName</c> / <c>Description</c> だけを持つ札で、
/// 実際の処理は <c>UseOption(player, option)</c> の中の <c>option.Is("gen_battleaxe")</c> という
/// 文字列比較の連鎖でした。ここでは札ではなく振る舞いを持つインスタンスなので、
/// 文字列 ID も分岐も存在しません。
/// </para>
/// </summary>
public abstract class AbilityChoice
{
    /// <summary>
    /// 表示名です。
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 説明です。
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    /// この選択肢を今使えるか。使えないなら理由を返します。
    /// </summary>
    public virtual bool CanUse(Player player, out string failureReason)
    {
        failureReason = null;

        return true;
    }

    /// <summary>
    /// この選択肢の効果です。
    /// </summary>
    public abstract void Use(Player player);
}
