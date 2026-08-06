using System;
using System.Collections.Generic;
using AntiMeme.Spawning.Waves;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Spawning;

/// <summary>
/// リスポーンウェーブ 1 種類です。<b>波 1 つ = クラス 1 つ。</b>
///
/// <para>
/// 中身は <see cref="SpawnSet"/> そのものです。役職構成の宣言も、必須枠の確保も、
/// 重み付き抽選も Sliced が持っているので、ここが足すのは
/// 「どの陣営の波か」「本隊か増援か」「何割を対象にするか」「テーマ曲は何か」だけです。
/// </para>
/// <para>
/// 旧実装は <c>SpawnTypeId</c> という 24 値の enum で波を同定し、
/// <c>UnitPack</c> がその enum → 役職表の辞書を持ち、
/// 陣営・比率・テーマ曲・重みがそれぞれ別の switch や辞書に散っていました。
/// すべてこのクラスのプロパティに畳んであります。
/// </para>
/// </summary>
/// <example>
/// <code>
/// public sealed class NtfNormalWave : WaveSet
/// {
///     public override string Name    => "MTF Epsilon-11";
///     public override Faction Faction => Faction.FoundationStaff;
///     public override int DefaultWeight => 80;
///     public override string Theme   => "./WaveThemes/_w_ntf.ogg";
///
///     public override IReadOnlyList&lt;SpawnSetRoleDefinition&gt; SpawnRoles =>
///     [
///         SpawnSetRoleDefinition.Vanilla(RoleTypeId.NtfCaptain, count: 1, isForced: true),
///         SpawnSetRoleDefinition.Custom&lt;NtfLieutenant&gt;(count: 2),
///     ];
/// }
/// </code>
/// </example>
public abstract class WaveSet : SpawnSet
{
    private List<Player> candidates;

    /// <summary>
    /// この波が属する陣営です。財団側か敵対側かで抽選プールが分かれます。
    /// </summary>
    public abstract Faction Faction { get; }

    /// <summary>
    /// 増援 (ミニウェーブ) かどうか。false なら本隊です。
    /// </summary>
    public virtual bool IsMiniWave => false;

    /// <summary>
    /// 抽選の重みです。0 なら通常は選ばれません(コンテキスト側で上書きできます)。
    /// </summary>
    public virtual int DefaultWeight => 0;

    /// <summary>
    /// 候補者のうち何割を実際に湧かせるかです。1 なら全員。
    /// </summary>
    public virtual float Ratio => 1f;

    /// <summary>
    /// 湧いたときに流すテーマ曲です。null なら鳴らしません。
    /// </summary>
    public virtual string Theme => null;

    /// <summary>
    /// 湧いたときのアナウンスです。既定では黙ります。
    /// 人数を読み上げる波があるので、湧いた人数を受け取ります。
    /// </summary>
    public virtual (string Cassie, string Subtitle) Announcement(int spawnCount) => default;

    /// <inheritdoc/>
    public override int AllowedPlayerCount
    {
        get
        {
            if (candidates is null) return -1;

            if (Ratio >= 1f) return candidates.Count;

            // 1 人も湧かないよりは 1 人だけでも湧かせる。
            return Mathf.Max(1, (int)Math.Truncate(candidates.Count * Ratio));
        }
    }

    /// <summary>
    /// 指定した候補者へこの波を割り当てます。
    /// </summary>
    /// <returns>実際に湧いた人数。</returns>
    public int SpawnFor(List<Player> waveCandidates)
    {
        candidates = waveCandidates;

        try
        {
            return Spawn();
        }
        finally
        {
            candidates = null;
        }
    }

    /// <inheritdoc/>
    protected override List<Player> TargetPlayers() =>
        candidates is null ? base.TargetPlayers() : [..candidates];
}
