using System;
using System.Collections.Generic;

namespace Sliced.API.Extensions;

public static class CollectionExtensions
{
    private static readonly Random Random = new Random();
    private static readonly object RandomLock = new object();

    /// <summary>
    /// List・配列など、IListをその場でシャッフルします。
    /// Fisher-Yatesアルゴリズムを使用します。
    /// </summary>
    public static void Shuffle<T>(this IList<T> list)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        lock (RandomLock)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Next(i + 1);

                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
    }

    /// <summary>
    /// 指定したRandomを使って、IListをその場でシャッフルします。
    /// シードを固定したい場合などに使用できます。
    /// </summary>
    public static void Shuffle<T>(this IList<T> list, Random random)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        if (random == null)
            throw new ArgumentNullException(nameof(random));

        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);

            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    /// <summary>
    /// 元のコレクションを変更せず、シャッフル済みのListを返します。
    /// </summary>
    public static List<T> Shuffled<T>(this IEnumerable<T> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var result = new List<T>(source);
        result.Shuffle();

        return result;
    }

    /// <summary>
    /// 指定したRandomを使い、シャッフル済みのListを返します。
    /// </summary>
    public static List<T> Shuffled<T>(
        this IEnumerable<T> source,
        Random random)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        if (random == null)
            throw new ArgumentNullException(nameof(random));

        var result = new List<T>(source);
        result.Shuffle(random);

        return result;
    }
}