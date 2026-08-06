using Scp914;

namespace AntiMeme.Items.Scp914;

/// <summary>
/// つまみの設定ごとの規則をまとめたものです。
/// 書かなかった設定はバニラの挙動に任せます(<see cref="Scp914Rule.Keep"/> ではありません)。
/// </summary>
/// <example>
/// <code>
/// public override Scp914RuleSet Scp914Rules => new()
/// {
///     Rough    = Scp914Rule.Destroy,
///     Coarse   = Scp914Rule.ToVanilla(ItemType.KeycardJanitor),
///     Fine     = Scp914Rule.To&lt;KeycardSiteDirector&gt;(),
///     VeryFine = Scp914Rule.Weighted(
///         (0.7f, Scp914Rule.To&lt;MasterCard&gt;()),
///         (0.3f, Scp914Rule.Destroy)),
/// };
/// </code>
/// </example>
public sealed class Scp914RuleSet
{
    /// <summary>粗い設定です。</summary>
    public Scp914Rule? Rough { get; init; }

    /// <summary>雑な設定です。</summary>
    public Scp914Rule? Coarse { get; init; }

    /// <summary>1:1 設定です。</summary>
    public Scp914Rule? OneToOne { get; init; }

    /// <summary>細かい設定です。</summary>
    public Scp914Rule? Fine { get; init; }

    /// <summary>非常に細かい設定です。</summary>
    public Scp914Rule? VeryFine { get; init; }

    /// <summary>
    /// 設定に対応する規則です。未指定なら null(= バニラに任せる)。
    /// </summary>
    public Scp914Rule? For(Scp914KnobSetting setting) => setting switch
    {
        Scp914KnobSetting.Rough => Rough,
        Scp914KnobSetting.Coarse => Coarse,
        Scp914KnobSetting.OneToOne => OneToOne,
        Scp914KnobSetting.Fine => Fine,
        Scp914KnobSetting.VeryFine => VeryFine,
        _ => null,
    };
}
