using System;
using System.Collections.Generic;
using System.Diagnostics;
using LabApi.Events.Handlers;
using MEC;
using ProjectMER.Features.Objects;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;
using Object = UnityEngine.Object;

namespace AntiMeme.Maps.Bridges;

/// <summary>
/// Unity でスキマティックに埋め込んだマーカー (<see cref="SchematicObjectPrefabObject"/>) を
/// <see cref="ObjectPrefab"/> の実体に結び付けるブリッジです。
///
/// <para>
/// マーカーの <c>PrefabType</c> は型名です。<see cref="ObjectPrefab.FindType"/> がそのまま解決するので、
/// キーやエイリアスを登録する表はありません。クラスを書けばマップエディタから置けます。
/// </para>
/// <para>
/// ラウンド開始時には数十個のマーカーが同じフレームで一斉に湧きます。それぞれがスキマティックを
/// 出すため、そのまま処理するとフレームが飛びます。フレームあたりの時間を区切って順に捌きます。
/// </para>
/// </summary>
public sealed class ObjectPrefabMarkerBridge : EventHandlerBase
{
    /// <summary>1 フレームで結び付けに使ってよい時間です。</summary>
    private const float FrameBudgetMs = 4f;

    /// <summary>マーカー出現からスキマティック側が落ち着くまでの猶予です。</summary>
    private const float SettleDelay = 0.1f;

    private static readonly List<PendingMarker> Pending = [];
    private static readonly HashSet<SchematicObjectPrefabObject> Queued = [];

    private static CoroutineHandle draining;
    private static int drainIndex;

    /// <inheritdoc/>
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        SchematicObjectPrefabObject.Spawned += OnMarkerSpawned;
        SchematicObjectPrefabObject.Destroyed += OnMarkerDestroyed;
        ServerEvents.WaitingForPlayers += SweepUnbound;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        SchematicObjectPrefabObject.Spawned -= OnMarkerSpawned;
        SchematicObjectPrefabObject.Destroyed -= OnMarkerDestroyed;
        ServerEvents.WaitingForPlayers -= SweepUnbound;

        Timing.KillCoroutines(draining);
        Pending.Clear();
        Queued.Clear();
        drainIndex = 0;
        MarkerBinding.ReleaseAll();
    }

    private static void OnMarkerSpawned(SchematicObjectPrefabObject marker)
    {
        // 結び付け済みのマーカーからの再通知は「更新された」の意味なので、キューに入れて再適用する。
        if (marker == null || !Queued.Add(marker)) return;

        Pending.Add(new PendingMarker(marker, Time.time + SettleDelay));

        if (!draining.IsRunning)
            draining = Timing.RunCoroutine(Drain());
    }

    private static void OnMarkerDestroyed(SchematicObjectPrefabObject marker) => Queued.Remove(marker);

    private static IEnumerator<float> Drain()
    {
        Stopwatch frame = Stopwatch.StartNew();

        // 待ち行列は追加されうるので、取り出しは先頭インデックスを進めるだけにする。
        while (drainIndex < Pending.Count)
        {
            PendingMarker pending = Pending[drainIndex];

            if (Time.time < pending.ReadyAt)
            {
                yield return Timing.WaitForOneFrame;
                frame.Restart();

                continue;
            }

            drainIndex++;
            Queued.Remove(pending.Marker);

            // Unity の破棄済みオブジェクトは is パターンをすり抜けるので == で確かめる。
            if (pending.Marker != null)
                Bind(pending.Marker);

            if (frame.Elapsed.TotalMilliseconds < FrameBudgetMs) continue;

            yield return Timing.WaitForOneFrame;
            frame.Restart();
        }

        Pending.Clear();
        drainIndex = 0;
    }

    /// <summary>
    /// 購読を始める前から置かれていたマーカー (プラグイン再読み込み時など) を 1 度だけ拾います。
    /// </summary>
    private static void SweepUnbound()
    {
        foreach (SchematicObjectPrefabObject marker in Object.FindObjectsByType<SchematicObjectPrefabObject>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (marker.GetComponent<MarkerBinding>() == null)
                OnMarkerSpawned(marker);
        }
    }

    private static void Bind(SchematicObjectPrefabObject marker)
    {
        if (string.IsNullOrWhiteSpace(marker.PrefabType)) return;

        if (ObjectPrefab.FindType(marker.PrefabType) is not { } prefabType)
        {
            Logger.Warn($"[ObjectPrefabMarkerBridge] ObjectPrefab '{marker.PrefabType}' が見つかりません。");

            return;
        }

        MarkerBinding binding = marker.GetComponent<MarkerBinding>();

        if (binding != null && binding.Prefab is { IsAlive: true } existing)
        {
            // 同じ型なら作り直さずに値と transform を当て直すだけ。
            if (existing.GetType() == prefabType)
            {
                Apply(existing, marker);
                existing.SyncTransform();

                return;
            }

            existing.Destroy();
        }

        ObjectPrefab prefab;

        // マップデータが指す型のコンストラクタは自分の管理外なので、ここだけは落ちても続行する。
        try
        {
            prefab = (ObjectPrefab)Activator.CreateInstance(prefabType);
        }
        catch (Exception exception)
        {
            Logger.Error($"[ObjectPrefabMarkerBridge] {prefabType.Name} を生成できませんでした: {exception}");

            return;
        }

        Apply(prefab, marker);

        // マーカーが寿命を持つので、マップデータとしての保存対象にはしない。
        prefab.IsSaveable = false;
        prefab.Create();

        if (binding == null)
            binding = marker.gameObject.AddComponent<MarkerBinding>();

        binding.Bind(prefab);
    }

    private static void Apply(ObjectPrefab prefab, SchematicObjectPrefabObject marker)
    {
        Transform markerTransform = marker.transform;

        // ProjectMER の Surface は屋外扱いで位置が絶対座標になる。移行済みの旧 aaa.json
        // 4 点だけ、対応する Surface ルームの TransformPoint を通して一度だけ正規化する。
        if (SurfaceMarkerPositionNormalizer.TryNormalize(marker, out Vector3 absolute))
            markerTransform.position = absolute;

        prefab.Position = markerTransform.position;
        prefab.Rotation = markerTransform.rotation;
        prefab.Scale = markerTransform.lossyScale;
        prefab.Lifespan = marker.AutoDestroyEnabled ? marker.AutoDestroyTime : -1f;
        prefab.Tag = marker.EffectiveTag;

        if (marker.Options.Count > 0)
            prefab.ApplyOptions(marker.Options);
    }

    private readonly struct PendingMarker
    {
        public PendingMarker(SchematicObjectPrefabObject marker, float readyAt)
        {
            Marker = marker;
            ReadyAt = readyAt;
        }

        public SchematicObjectPrefabObject Marker { get; }

        public float ReadyAt { get; }
    }
}

/// <summary>
/// マーカーの GameObject に付き、Prefab をマーカーの transform に追従させます。
/// マーカーが消えれば Prefab も消えます。
/// </summary>
public sealed class MarkerBinding : MonoBehaviour
{
    private static readonly List<MarkerBinding> Active = [];

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 lastScale;

    /// <summary>
    /// このマーカーに結び付いた Prefab です。
    /// </summary>
    public ObjectPrefab Prefab { get; private set; }

    /// <summary>
    /// すべての結び付きを解いて Prefab を破棄します。
    /// </summary>
    public static void ReleaseAll()
    {
        foreach (MarkerBinding binding in Active.ToArray())
        {
            binding.Prefab?.Destroy();
            binding.Prefab = null;
            Destroy(binding);
        }
    }

    /// <summary>
    /// Prefab を結び付けます。
    /// </summary>
    public void Bind(ObjectPrefab prefab)
    {
        Prefab = prefab;
        Cache();
    }

    private void Awake() => Active.Add(this);

    private void OnDestroy()
    {
        Active.Remove(this);

        ObjectPrefab prefab = Prefab;
        Prefab = null;
        prefab?.Destroy();
    }

    private void LateUpdate()
    {
        if (Prefab is not { IsAlive: true } prefab)
        {
            // 外から破棄された Prefab は追わない。
            Prefab = null;

            return;
        }

        if (!prefab.FollowsMarker) return;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 scale = transform.lossyScale;

        if (position == lastPosition && rotation == lastRotation && scale == lastScale) return;

        lastPosition = position;
        lastRotation = rotation;
        lastScale = scale;

        prefab.Position = position;
        prefab.Rotation = rotation;
        prefab.Scale = scale;
    }

    private void Cache()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastScale = transform.lossyScale;
    }
}
