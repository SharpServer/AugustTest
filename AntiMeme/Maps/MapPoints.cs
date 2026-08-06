using System;
using System.Collections.Generic;
using LabApi.Events.Handlers;
using MEC;
using ProjectMER.Events.Arguments;
using ProjectMER.Features;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using SchematicEvents = ProjectMER.Events.Handlers.Schematic;

namespace AntiMeme.Maps;

/// <summary>
/// マップに置かれたトリガーポイントを、名札から座標で引ける形にしたものです。
///
/// <para>
/// 旧実装は「タグ名 → 静的フィールド」の対応表を 100 行の switch で書き、
/// <c>MapFlags</c> に 30 個の <c>Vector3</c> を並べていました。タグを 1 つ増やすたびに
/// フィールドと switch の両方を触る必要があり、値が入っていない状態と
/// 「原点にあるポイント」を区別できませんでした。
/// ここでは名札をそのままキーにして引くので、追加時に触る場所はありません。
/// </para>
/// <para>
/// 走査は <see cref="Object.FindObjectsByType"/> を含むので、スキマティックのスポーンごとに
/// 呼ぶと重くなります。更新は <see cref="MapPointsRefresher"/> がまとめて行い、結果をここに置きます。
/// </para>
/// </summary>
/// <example>
/// <code>
/// if (MapPoints.TryGet("Scp173SpawnPoint", out Vector3 spawn))
///     player.Position = spawn;
///
/// foreach (Vector3 escape in MapPoints.All("EscapePoint"))
///     ...
/// </code>
/// </example>
public static class MapPoints
{
    private static readonly Dictionary<string, List<TriggerPointInfo>> ByTag =
        new Dictionary<string, List<TriggerPointInfo>>(StringComparer.OrdinalIgnoreCase);

    private static readonly TriggerPointInfo[] Empty = [];

    /// <summary>
    /// 名札の付いたポイントが 1 つでも見つかっているかどうか。
    /// </summary>
    public static bool Has(string tag) => ByTag.ContainsKey(tag);

    /// <summary>
    /// 名札に対応するポイントを 1 つ引きます。
    /// </summary>
    public static bool TryGet(string tag, out Vector3 position)
    {
        if (TryGetPoint(tag, out TriggerPointInfo point))
        {
            position = point.Position;

            return true;
        }

        position = Vector3.zero;

        return false;
    }

    /// <summary>
    /// 名札に対応するポイントを、向き付きで 1 つ引きます。
    /// </summary>
    public static bool TryGet(string tag, out Vector3 position, out Quaternion rotation)
    {
        if (TryGetPoint(tag, out TriggerPointInfo point))
        {
            position = point.Position;
            rotation = point.Rotation;

            return true;
        }

        position = Vector3.zero;
        rotation = Quaternion.identity;

        return false;
    }

    /// <summary>
    /// 名札に対応するポイント情報を 1 つ引きます。
    /// </summary>
    public static bool TryGetPoint(string tag, out TriggerPointInfo point)
    {
        if (!string.IsNullOrWhiteSpace(tag) &&
            ByTag.TryGetValue(tag.Trim(), out List<TriggerPointInfo> points) &&
            points.Count > 0)
        {
            point = points[0];

            return true;
        }

        point = null;

        return false;
    }

    /// <summary>
    /// 同じ名札のポイントをすべて返します (脱出地点のように複数置くもの向け)。
    /// </summary>
    public static IReadOnlyList<TriggerPointInfo> All(string tag)
        => !string.IsNullOrWhiteSpace(tag) && ByTag.TryGetValue(tag.Trim(), out List<TriggerPointInfo> points)
            ? points
            : Empty;

    /// <summary>
    /// マップ上のトリガーポイントを走査し直します。
    /// </summary>
    public static void Refresh()
    {
        ByTag.Clear();

        foreach (TriggerPointInfo point in TriggerPointManager.GetAllPoints())
        {
            if (string.IsNullOrWhiteSpace(point.Tag)) continue;

            string key = point.Tag.Trim();

            if (!ByTag.TryGetValue(key, out List<TriggerPointInfo> points))
                ByTag[key] = points = [];

            points.Add(point);
        }
    }
}

/// <summary>
/// <see cref="MapPoints"/> の中身を更新するハンドラです。
/// スキマティックが連続でスポーンする間は走査をまとめたいので、最後の 1 件から少し待ってから走ります。
/// </summary>
public sealed class MapPointsRefresher : EventHandlerBase
{
    private const float SchematicSettleDelay = 0.1f;
    private const float MapLoadSettleDelay = 5f;

    private CoroutineHandle pending;

    /// <inheritdoc/>
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        SchematicEvents.SchematicSpawned += OnSchematicSpawned;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        SchematicEvents.SchematicSpawned -= OnSchematicSpawned;

        Timing.KillCoroutines(pending);
    }

    private void OnWaitingForPlayers() => Schedule(MapLoadSettleDelay);

    private void OnSchematicSpawned(SchematicSpawnedEventArgs ev) => Schedule(SchematicSettleDelay);

    private void Schedule(float delay)
    {
        Timing.KillCoroutines(pending);
        pending = Timing.CallDelayed(delay, MapPoints.Refresh);
    }
}
