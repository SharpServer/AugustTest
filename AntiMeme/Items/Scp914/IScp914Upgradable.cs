namespace AntiMeme.Items.Scp914;

/// <summary>
/// SCP-914 での変換規則を自分で持つカスタムアイテムです。
///
/// <see cref="Sliced.API.Features.CustomItem"/> の派生クラスにこれを実装するだけで
/// <see cref="Scp914Handler"/> が拾います。登録も ID も要りません。
/// </summary>
/// <example>
/// <code>
/// public sealed class KeycardJanitorPlus : CustomKeycard, IScp914Upgradable
/// {
///     public Scp914RuleSet Scp914Rules => new()
///     {
///         Rough = Scp914Rule.Destroy,
///         Fine  = Scp914Rule.To&lt;KeycardSiteDirector&gt;(),
///     };
/// }
/// </code>
/// </example>
public interface IScp914Upgradable
{
    /// <summary>
    /// つまみの設定ごとの変換規則です。
    /// </summary>
    Scp914RuleSet Scp914Rules { get; }
}
