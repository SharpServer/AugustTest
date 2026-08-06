using System;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Scp914;

/// <summary>
/// SCP-914 に入れたアイテムをどうするか、という 1 つの規則です。
///
/// <b>規則そのものが処理を持ちます。</b> 旧実装は <c>Scp914RuleKind</c> enum と
/// 7 つの nullable フィールドを持つタグ付き共用体で、dispatcher 側に巨大な switch がありました。
/// ここでは各ファクトリが自分の振る舞いを閉じ込めるので、enum も switch も要りません。
/// </summary>
/// <example>
/// <code>
/// // 30% で消滅、それ以外は上位カードへ
/// Scp914Rule.Destroy.WithChance(0.3f)
///
/// // 重み付き抽選。合計が 1 未満なら残りは「そのまま」
/// Scp914Rule.Weighted(
///     (0.5f, Scp914Rule.To&lt;KeycardSiteDirector&gt;()),
///     (0.2f, Scp914Rule.Destroy))
/// </code>
/// </example>
public readonly struct Scp914Rule
{
    private static readonly System.Random Random = new System.Random();

    private readonly Action<Scp914Context> apply;

    private Scp914Rule(Action<Scp914Context> apply, float chance)
    {
        this.apply = apply;
        Chance = chance;
    }

    /// <summary>
    /// この規則が適用される確率です (0〜1)。外れた場合は何もしません。
    /// </summary>
    public float Chance { get; }

    /// <summary>
    /// アイテムを消します。
    /// </summary>
    public static Scp914Rule Destroy => new Scp914Rule(context => context.Consume(), 1f);

    /// <summary>
    /// 何もしません。バニラの変換も走りません。
    /// </summary>
    public static Scp914Rule Keep => new Scp914Rule(null, 1f);

    /// <summary>
    /// バニラアイテムに変換します。
    /// </summary>
    public static Scp914Rule ToVanilla(ItemType type, int count = 1)
    {
        return new Scp914Rule(
            context =>
            {
                context.Consume();

                for (int i = 0; i < count; i++)
                {
                    context.Deliver(type);
                }
            },
            1f);
    }

    /// <summary>
    /// 状況に応じて出力を選びます。
    /// </summary>
    public static Scp914Rule ToVanilla(Func<Scp914Context, ItemType> selector, int count = 1)
    {
        return new Scp914Rule(
            context =>
            {
                ItemType type = selector(context);

                context.Consume();

                for (int i = 0; i < count; i++)
                {
                    context.Deliver(type);
                }
            },
            1f);
    }

    /// <summary>
    /// カスタムアイテムに変換します。型で指定します。
    /// </summary>
    public static Scp914Rule To<T>(int count = 1) where T : CustomItem, new()
    {
        return new Scp914Rule(
            context =>
            {
                bool toInventory = context.IsInventory && !context.Owner.IsInventoryFull;
                Vector3 position = context.OutputPosition;

                context.Consume();

                for (int i = 0; i < count; i++)
                {
                    if (toInventory)
                        CustomItem.Give<T>(context.Owner);
                    else
                        CustomItem.Spawn<T>(position);
                }
            },
            1f);
    }

    /// <summary>
    /// 好きな処理を書きます。
    /// </summary>
    public static Scp914Rule Custom(Action<Scp914Context> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        return new Scp914Rule(action, 1f);
    }

    /// <summary>
    /// 相対重みで子ルールを 1 つ選びます。
    /// 重みの合計が 1 未満なら、残りの確率は「そのまま」になります。
    /// </summary>
    public static Scp914Rule Weighted(params (float Weight, Scp914Rule Rule)[] entries)
    {
        if (entries is null || entries.Length == 0)
            return Keep;

        return new Scp914Rule(
            context =>
            {
                float total = 0f;

                foreach ((float weight, Scp914Rule _) in entries)
                {
                    total += Mathf.Max(0f, weight);
                }

                // 合計が 1 未満なら残りは「そのまま」に割り当てる。1 を超えるなら正規化。
                float roll = (float)Random.NextDouble() * Mathf.Max(1f, total);

                foreach ((float weight, Scp914Rule rule) in entries)
                {
                    roll -= Mathf.Max(0f, weight);

                    if (roll < 0f)
                    {
                        rule.Execute(context);

                        return;
                    }
                }
            },
            1f);
    }

    /// <summary>
    /// 適用確率を変えた規則を返します。
    /// </summary>
    public Scp914Rule WithChance(float chance) => new Scp914Rule(apply, Mathf.Clamp01(chance));

    /// <summary>
    /// この規則を実行します。確率で外れた場合や <see cref="Keep"/> の場合は何もしません。
    /// </summary>
    public void Execute(Scp914Context context)
    {
        if (apply is null) return;
        if (Chance < 1f && Random.NextDouble() >= Chance) return;

        apply(context);
    }
}
