using System;
using System.Collections.Generic;
using System.Linq;
using PlayerRoles;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;
using AntiMeme.Spawning.Waves;

namespace AntiMeme.Spawning;

/// <summary>
/// ウェーブ抽選の文脈です。<b>文脈 1 つ = クラス 1 つ。</b>
///
/// <para>
/// 各波の重みは <see cref="WaveSet.DefaultWeight"/> が既定値で、
/// 文脈が変えたいものだけ <see cref="WeightOf"/> で上書きします。
/// 旧実装は「陣営 × 本隊/増援」の 4 本の <c>Dictionary&lt;SpawnTypeId,int&gt;</c> を
/// 文脈ごとに持ち、さらに <c>UnitPack</c> の役職表をマージしていました。
/// 波が自分の構成と既定の重みを持つので、どちらも要りません。
/// </para>
/// </summary>
/// <example>
/// <code>
/// public sealed class FacilityTerminationContext : SpawnContext
/// {
///     public override string Name => "Facility Termination";
///
///     public override int WeightOf(WaveSet wave) => wave switch
///     {
///         LastOperationWave or LastOperationBackupWave => 100,
///         GoCWave or GoCBackupWave => 70,
///         NtfWave or NtfBackupWave => 0,
///         _ => base.WeightOf(wave),
///     };
/// }
/// </code>
/// </example>
public abstract class SpawnContext
{
    private static WaveSet[] waves;

    /// <summary>
    /// 現在の文脈です。既定は <see cref="DefaultContext"/>。
    /// </summary>
    public static SpawnContext Active { get; private set; } = new DefaultContext();

    /// <summary>
    /// 表示名です。
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 宣言されている波をすべて返します。波は状態を持たない宣言なので使い回します。
    /// </summary>
    public static IReadOnlyList<WaveSet> AllWaves => waves ??= BuildWaves();

    /// <summary>
    /// 文脈を切り替えます。
    /// </summary>
    public static void Activate<T>() where T : SpawnContext, new() => Active = new T();

    /// <summary>
    /// 既定の文脈へ戻します。
    /// </summary>
    public static void Reset() => Active = new DefaultContext();

    /// <summary>
    /// 型で波を引きます。波は使い回しなので、毎回同じインスタンスが返ります。
    /// </summary>
    public static WaveSet Wave<T>() where T : WaveSet =>
        AllWaves.FirstOrDefault(wave => wave is T);

    /// <summary>
    /// 型名から波を引きます。RA コマンドやゲームモードから使います。
    /// </summary>
    public static WaveSet Find(string name) =>
        TypeParser.TryParse<WaveSet>(name, out Type type)
            ? AllWaves.FirstOrDefault(wave => wave.GetType() == type)
            : null;

    /// <summary>
    /// この文脈での抽選重みです。既定は波が持つ値。
    /// </summary>
    public virtual int WeightOf(WaveSet wave) => wave?.DefaultWeight ?? 0;

    /// <summary>
    /// 指定した陣営・種別の波を重み付きで 1 つ選びます。候補が無ければ null。
    /// </summary>
    public WaveSet Roll(Faction faction, bool miniWave, Func<int> chooseIndex)
    {
        List<WaveSet> candidates = AllWaves
            .Where(wave => wave.Faction == faction && wave.IsMiniWave == miniWave && WeightOf(wave) > 0)
            .ToList();

        if (candidates.Count == 0) return null;

        int total = candidates.Sum(WeightOf);
        int roll = chooseIndex is null ? UnityEngine.Random.Range(0, total) : chooseIndex();

        foreach (WaveSet wave in candidates)
        {
            int weight = WeightOf(wave);

            if (roll < weight) return wave;

            roll -= weight;
        }

        return candidates[0];
    }

    private static WaveSet[] BuildWaves()
    {
        List<WaveSet> collected = [];

        foreach (Type type in TypeParser.FindTypes<WaveSet>())
        {
            try
            {
                collected.Add((WaveSet)Activator.CreateInstance(type));
            }
            catch (Exception exception)
            {
                Logger.Error($"[AntiMeme] ウェーブ {type.FullName} を生成できませんでした: {exception}");
            }
        }

        return collected.ToArray();
    }
}
