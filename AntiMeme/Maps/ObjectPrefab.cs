using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Maps;

/// <summary>
/// マップ上に置かれる「振る舞いを持つ物体」の基底です。
///
/// <para>
/// 1 つの ObjectPrefab は「スキマティック 1 個 + そこに埋め込まれた Interactable 群 + 状態」を
/// まとめて所有します。<see cref="SchematicName"/> を宣言しておけば <see cref="Create"/> が
/// スキマティックを出し、その中で <c>ObjectPrefabKey</c> が付いたブロックを自動で管理下に取り込みます。
/// </para>
/// <para>
/// 寿命は <see cref="RoundScope"/> に載っています。<see cref="Create"/> した時点でラウンド終了時の
/// 破棄が予約されるので、派生クラスが <c>RoundRestarting</c> を購読する必要はありません。
/// 遅延処理も <see cref="Delay"/> / <see cref="Loop"/> 経由なら破棄時にまとめて止まります。
/// </para>
/// <para>
/// 同一性は型です。文字列キーのレジストリはありません。マップ側から文字列で指す必要がある場所
/// (ProjectMER のマーカーの <c>PrefabType</c>) は <see cref="FindType"/> が型名で解決します。
/// <see cref="Tag"/> は「型」ではなく「この個体」を指す、マップ作者が付ける名札です。
/// </para>
/// </summary>
/// <example>
/// <code>
/// public sealed class Vent : ObjectPrefab
/// {
///     protected override string SchematicName => "Vent";
///
///     protected override void OnSetup()
///     {
///         GetInteractable("Lid").Interacted += ev => ev.Player.SendHint("開いた");
///     }
/// }
///
/// new Vent { Position = point, Tag = "EzVent" }.Create();
/// </code>
/// </example>
public abstract class ObjectPrefab
{
    private static readonly List<ObjectPrefab> LivingInstances = [];

    // TypeParser は毎回 AppDomain 全体を舐めるので、マーカー 1 個ごとに呼ぶと round start が詰まる。
    // 「見つからなかった」も覚える (null を入れる) ので、綴り間違いのマーカーが並んでも再走査しない。
    private static readonly Dictionary<string, Type> ResolvedTypes =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    private static Type[] knownTypes;

    private readonly List<InteractableHandle> interactables = [];
    private readonly Dictionary<string, SchematicBlock> managedBlocks =
        new Dictionary<string, SchematicBlock>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<SchematicBlock, Vector3> shownLocalPositions = new Dictionary<SchematicBlock, Vector3>();
    private readonly List<CoroutineHandle> scheduled = [];

    private Vector3 position = Vector3.zero;
    private Quaternion rotation = Quaternion.identity;
    private Vector3 scale = Vector3.one;
    private string tag = string.Empty;

    /// <summary>
    /// 生きている ObjectPrefab をすべて返します。
    /// </summary>
    public static IReadOnlyList<ObjectPrefab> All => LivingInstances;

    /// <summary>
    /// 引数なしで生成できる ObjectPrefab の具象型一覧です。マップエディタ向けのカタログに使います。
    /// </summary>
    public static IReadOnlyList<Type> KnownTypes =>
        knownTypes ??= TypeParser.FindTypes<ObjectPrefab>().ToArray();

    /// <summary>
    /// <see cref="Create"/> 済みで、まだ <see cref="Destroy"/> されていないかどうか。
    /// </summary>
    public bool IsAlive { get; private set; }

    /// <summary>
    /// この個体に付いた名札です。マップ作者がマーカーに書き、
    /// <see cref="Find{T}(string)"/> で他の Prefab から引けます。
    /// </summary>
    public string Tag
    {
        get => tag;
        set => tag = value is null ? string.Empty : value.Trim();
    }

    /// <summary>
    /// ワールド座標です。設定するとスキマティックと Interactable が追従します。
    /// </summary>
    public Vector3 Position
    {
        get => position;
        set
        {
            position = value;
            ApplyTransform();
        }
    }

    /// <summary>
    /// ワールド回転です。
    /// </summary>
    public Quaternion Rotation
    {
        get => rotation;
        set
        {
            rotation = value;
            ApplyTransform();
        }
    }

    /// <summary>
    /// ワールドスケールです。
    /// </summary>
    public Vector3 Scale
    {
        get => scale;
        set
        {
            scale = value;
            ApplyTransform();
        }
    }

    /// <summary>
    /// 生成から自動で消えるまでの秒数です。0 以下なら自動で消えません。
    /// </summary>
    public float Lifespan { get; set; } = -1f;

    /// <summary>
    /// マップデータとして保存・再読み込みの対象になるかどうか。
    /// マーカー由来の個体はマーカーが寿命を持つので false になります。
    /// </summary>
    public bool IsSaveable { get; set; } = true;

    /// <summary>
    /// マーカーを動かしたときに追従するかどうか。自分で座標を動かす Prefab は false にします。
    /// </summary>
    public virtual bool FollowsMarker => true;

    /// <summary>
    /// この Prefab が出しているスキマティックです。出していなければ null。
    /// </summary>
    public SchematicObject Schematic { get; private set; }

    /// <summary>
    /// この Prefab が管理している Interactable の一覧です。
    /// </summary>
    public IReadOnlyList<InteractableHandle> Interactables => interactables;

    /// <summary>
    /// <see cref="Create"/> で自動的にスポーンさせるスキマティック名です。
    /// null / 空なら何も出しません。実行中に切り替えたい場合は getter で分岐して
    /// <see cref="SpawnSchematic"/> を呼び直します。
    /// </summary>
    protected virtual string SchematicName => null;

    /// <summary>
    /// <see cref="Create"/> から <see cref="OnSetup"/> までの待ち時間です。
    /// スキマティックのブロックが揃うのを待つためのもので、0 以下なら即時に呼ばれます。
    /// </summary>
    protected virtual float SetupDelay => 0.5f;

    /// <summary>
    /// 出したスキマティックが当たり判定を持つかどうか。
    /// false にすると Collider と <c>PrimitiveFlags.Collidable</c> を全部落とします。
    /// </summary>
    /// <remarks>
    /// 見た目だけの飾りや、当たり判定を別の実体 (不可視 NPC・Interactable) が受け持つ
    /// オブジェクトはここを false にしてください。true のままだと、
    /// 触手のようにモデルが太いものにプレイヤーが引っ掛かります。
    /// </remarks>
    protected virtual bool SchematicCollides => true;

    /// <summary>
    /// 型名 (クラス名または完全修飾名) から ObjectPrefab の具象型を引きます。見つからなければ null。
    /// </summary>
    public static Type FindType(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        string key = typeName.Trim();

        if (ResolvedTypes.TryGetValue(key, out Type cached)) return cached;

        TypeParser.TryParse<ObjectPrefab>(key, out Type resolved);
        ResolvedTypes[key] = resolved;

        return resolved;
    }

    /// <summary>
    /// 指定した名札を持つ、生きている <typeparamref name="T"/> をすべて返します (派生型を含む)。
    /// </summary>
    public static IEnumerable<T> Find<T>(string tag) where T : ObjectPrefab
    {
        if (string.IsNullOrWhiteSpace(tag)) return [];

        string normalized = tag.Trim();

        return LivingInstances
            .OfType<T>()
            .Where(prefab => string.Equals(prefab.Tag, normalized, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// 指定した名札を持つ <typeparamref name="T"/> を 1 つ返します。無ければ null。
    /// </summary>
    public static T FindOne<T>(string tag) where T : ObjectPrefab => Find<T>(tag).FirstOrDefault();

    /// <summary>
    /// この Prefab を実体化します。二重に呼んでも 1 回しか効きません。
    /// </summary>
    public ObjectPrefab Create()
    {
        if (IsAlive) return this;

        IsAlive = true;
        LivingInstances.Add(this);

        // ラウンド後始末の入口はここ 1 つ。派生クラスは RoundRestarting を購読しない。
        RoundScope.Current.OnEnd(Destroy);

        if (Lifespan > 0f)
            Delay(Lifespan, Destroy);

        if (SchematicName is { Length: > 0 } schematicName)
            SpawnSchematic(schematicName);

        OnCreate();

        if (SetupDelay > 0f)
            Delay(SetupDelay, OnSetup);
        else
            OnSetup();

        return this;
    }

    /// <summary>
    /// この Prefab と、それが出したスキマティック・Interactable・遅延処理をすべて片付けます。
    /// 二重に呼んでも安全です。
    /// </summary>
    public void Destroy()
    {
        if (!IsAlive) return;

        IsAlive = false;
        LivingInstances.Remove(this);

        foreach (CoroutineHandle handle in scheduled)
        {
            Timing.KillCoroutines(handle);
        }

        scheduled.Clear();

        // 後始末は 1 つ落ちても残りを巻き込まない。
        try
        {
            OnDestroy();
        }
        catch (Exception exception)
        {
            Logger.Error($"[{GetType().Name}] OnDestroy で例外が発生しました: {exception}");
        }

        ClearInteractables(handle => true, destroyToys: true);
        DespawnSchematic();
    }

    /// <summary>
    /// スキマティックと Interactable の transform を現在の座標へ合わせ直します。
    /// </summary>
    public void SyncTransform()
    {
        if (!IsAlive) return;

        if (Schematic != null)
        {
            Schematic.Position = position;
            Schematic.Rotation = rotation;
            Schematic.Scale = scale;
        }

        foreach (InteractableHandle handle in interactables)
        {
            // スキマティックから取り込んだ分は親子関係で追従済み。
            if (!handle.SyncsTransform || handle.Toy.IsDestroyed) continue;

            Transform toyTransform = handle.Toy.Transform;
            toyTransform.localPosition = position + (rotation * handle.LocalOffset);
            toyTransform.localRotation = rotation;
            toyTransform.localScale = Vector3.Scale(handle.BaseScale, scale);

            InvisibleInteractableToy toyBase = handle.Toy.Base;
            toyBase.NetworkPosition = toyTransform.localPosition;
            toyBase.NetworkRotation = toyTransform.localRotation;
            toyBase.NetworkScale = toyTransform.localScale;
        }
    }

    /// <summary>
    /// マーカーやマップデータの Options を反映します。
    /// キー名を読み替えたい場合だけ override します。
    /// </summary>
    public virtual void ApplyOptions(IReadOnlyDictionary<string, string> options)
        => ObjectPrefabOptions.Apply(this, options);

    /// <summary>
    /// 現在値を Options 形式で書き出します。
    /// </summary>
    public virtual Dictionary<string, string> CollectOptions() => ObjectPrefabOptions.Collect(this);

    /// <summary>
    /// <see cref="Create"/> の直後、スキマティックを出した後に呼ばれます。
    /// </summary>
    protected virtual void OnCreate()
    {
    }

    /// <summary>
    /// <see cref="SetupDelay"/> 経過後に 1 回だけ呼ばれます。
    /// スキマティックのブロックを引いて Interactable を配線するのはここです。
    /// </summary>
    protected virtual void OnSetup()
    {
    }

    /// <summary>
    /// 破棄されるときに呼ばれます。スキマティックと Interactable はこの後に自動で片付きます。
    /// </summary>
    protected virtual void OnDestroy()
    {
    }

    /// <summary>
    /// 座標・回転・スケールが変わった後に呼ばれます。追従先が他にもある場合に使います。
    /// </summary>
    protected virtual void OnTransformUpdated()
    {
    }

    /// <summary>
    /// この Prefab のどれかの Interactable が長押しされ始めたときに呼ばれます。
    /// <c>ev.IsAllowed = false</c> で止められます。
    /// </summary>
    protected internal virtual void OnInteracting(PlayerSearchingToyEventArgs ev)
    {
    }

    /// <summary>
    /// この Prefab のどれかの Interactable の操作が完了したときに呼ばれます。
    /// 長押し時間 0 の即時操作も含みます。
    /// </summary>
    protected internal virtual void OnInteracted(PlayerSearchedToyEventArgs ev)
    {
    }

    /// <summary>
    /// スキマティックを出し直します。前のものは破棄されます。
    /// </summary>
    protected SchematicObject SpawnSchematic(string schematicName)
    {
        DespawnSchematic();

        Schematic = ObjectSpawner.SpawnSchematic(schematicName, position, rotation);

        if (Schematic == null)
        {
            Logger.Warn($"[{GetType().Name}] スキマティック '{schematicName}' を出せませんでした。");

            return null;
        }

        Schematic.Scale = scale;
        AdoptBlocks();
        SyncTransform();

        if (!SchematicCollides)
            DisableCollision(Schematic);

        return Schematic;
    }

    /// <summary>
    /// スキマティック配下の当たり判定を全部落とします。
    /// </summary>
    /// <remarks>
    /// Collider を切るだけではプリミティブがサーバー側で当たり続けるので、
    /// <c>PrimitiveObjectToy</c> の <c>Collidable</c> も一緒に外します。
    /// </remarks>
    private static void DisableCollision(SchematicObject schematic)
    {
        foreach (AdminToys.PrimitiveObjectToy primitive in
                 schematic.GetComponentsInChildren<AdminToys.PrimitiveObjectToy>(true))
        {
            primitive.NetworkPrimitiveFlags &= ~PrimitiveFlags.Collidable;
        }

        foreach (Collider collider in schematic.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }

    /// <summary>
    /// 出しているスキマティックを破棄します。
    /// </summary>
    protected void DespawnSchematic()
    {
        // 取り込んだ Interactable はスキマティック側が寿命を持つので、Toy は壊さず管理から外すだけ。
        ClearInteractables(handle => !handle.OwnsToy, destroyToys: false);
        managedBlocks.Clear();
        shownLocalPositions.Clear();

        SchematicObject schematic = Schematic;
        Schematic = null;

        if (schematic == null) return;

        schematic.Destroy();
    }

    /// <summary>
    /// <c>ObjectPrefabKey</c> で取り込んだブロックを引きます。無ければ null。
    /// </summary>
    protected SchematicBlock GetBlock(string key)
        => managedBlocks.TryGetValue(key, out SchematicBlock block) && block != null ? block : null;

    /// <summary>
    /// <c>ObjectPrefabKey</c> で取り込んだ Interactable を引きます。無ければ null。
    /// </summary>
    protected InteractableHandle GetInteractable(string key)
        => interactables.FirstOrDefault(handle => string.Equals(handle.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 指定ブロックのサブツリーに属する Interactable を引きます。
    /// 同じキーの Interactable がモデルごとに複数ある場合の区別に使います。
    /// </summary>
    protected InteractableHandle GetInteractableInBlock(string blockKey, string interactableKey = null)
    {
        if (FindBlock(blockKey) is not { } block) return null;

        Transform root = block.transform;

        return interactables.FirstOrDefault(handle =>
            !handle.Toy.IsDestroyed &&
            (string.IsNullOrWhiteSpace(interactableKey) ||
             string.Equals(handle.Key, interactableKey, StringComparison.OrdinalIgnoreCase)) &&
            handle.Toy.Transform.IsChildOf(root));
    }

    /// <summary>
    /// 他の取り込みブロックの下にぶら下がっていない、Prefab 直下の Interactable を引きます。
    /// </summary>
    protected InteractableHandle GetStandaloneInteractable(string key)
        => interactables.FirstOrDefault(handle =>
            !handle.Toy.IsDestroyed &&
            string.Equals(handle.Key, key, StringComparison.OrdinalIgnoreCase) &&
            !managedBlocks.Values.Any(block =>
                block != null &&
                block.transform != handle.Toy.Transform &&
                handle.Toy.Transform.IsChildOf(block.transform)));

    /// <summary>
    /// ブロックのサブツリーをネットワーク上で見せる / 隠します。
    /// サーバー側の実体は残るので何度でも切り替えられます。
    /// </summary>
    /// <param name="hiddenOffset">
    /// 隠している間の退避オフセット。UnSpawn してもサーバー側の Collider は残るので、
    /// 当たり判定ごと消したい場合に指定します。
    /// </param>
    protected bool SetBlockVisible(string keyOrName, bool visible, Vector3? hiddenOffset = null)
    {
        if (FindBlock(keyOrName) is not { } block) return false;

        if (hiddenOffset is { } offset)
        {
            if (!shownLocalPositions.TryGetValue(block, out Vector3 shownPosition))
            {
                shownPosition = block.transform.localPosition;
                shownLocalPositions[block] = shownPosition;
            }

            block.transform.localPosition = visible ? shownPosition : shownPosition + offset;
        }

        SetSubtreeSpawned(block, visible);

        return true;
    }

    /// <summary>
    /// 取り込みブロックの下から名前が完全一致する子ブロックを探し、見せる / 隠します。
    /// </summary>
    protected bool SetChildBlockVisible(string parentKeyOrName, string childName, bool visible)
    {
        if (FindBlock(parentKeyOrName) is not { } parent || string.IsNullOrWhiteSpace(childName)) return false;

        SchematicBlock child = parent.GetComponentsInChildren<SchematicBlock>(true)
            .FirstOrDefault(candidate =>
                candidate != parent &&
                string.Equals(candidate.name, childName, StringComparison.OrdinalIgnoreCase));

        if (child == null) return false;

        SetSubtreeSpawned(child, visible);

        return true;
    }

    /// <summary>
    /// Interactable を 1 つ作って管理下に置きます。位置とスケールは Prefab に追従します。
    /// </summary>
    protected InteractableHandle AddInteractable(
        float duration = 3f,
        Vector3? offset = null,
        Vector3? scale = null,
        InvisibleInteractableToy.ColliderShape shape = InvisibleInteractableToy.ColliderShape.Box)
    {
        InteractableToy toy = InteractableToy.Create(networkSpawn: false);
        toy.InteractionDuration = duration;
        toy.Shape = shape;

        InteractableHandle handle = new InteractableHandle(this, toy, offset ?? Vector3.zero, scale ?? Vector3.one);
        interactables.Add(handle);

        SyncTransform();
        toy.Spawn();

        return handle;
    }

    /// <summary>
    /// Interactable のサーバー側 Collider を切ります。
    /// クライアントの照準判定は SyncVar 側なので残り、プレイヤーが押し戻されなくなります。
    /// Collider は Shape の SyncVar 適用時に作り直されるので、スポーン直後は 1 度置いて掛け直します。
    /// </summary>
    protected void DisableServerCollision(InteractableHandle handle, bool retryAfterSpawn = true)
    {
        Disable();

        if (retryAfterSpawn)
            Delay(0.1f, Disable);

        void Disable()
        {
            if (handle.Toy.IsDestroyed) return;

            Collider collider = handle.Toy.Base._collider;

            if (collider != null)
                collider.enabled = false;
        }
    }

    /// <summary>
    /// 指定秒後に 1 回だけ実行します。この Prefab が破棄されるかラウンドが終われば実行されません。
    /// </summary>
    protected CoroutineHandle Delay(float seconds, Action action)
    {
        // IsAlive の確認だけで足りる。旧実装のような「インスタンス ID を引き直して同一性を検証する」
        // 再取得は不要 (破棄時に scheduled のハンドルを Kill し、ラウンド終了は RoundScope が閉じる)。
        CoroutineHandle handle = RoundScope.Current.Delay(seconds, () =>
        {
            if (IsAlive) action();
        });

        scheduled.Add(handle);

        return handle;
    }

    /// <summary>
    /// 一定間隔で実行し続けます。破棄またはラウンド終了で止まります。
    /// </summary>
    protected CoroutineHandle Loop(float interval, Action tick)
    {
        CoroutineHandle handle = RoundScope.Current.RunLoop(interval, () =>
        {
            if (IsAlive) tick();
        });

        scheduled.Add(handle);

        return handle;
    }

    /// <summary>
    /// この物体を <paramref name="from"/> から <paramref name="to"/> へ
    /// <paramref name="seconds"/> 秒かけて動かします。
    /// 着いたら <paramref name="onArrive"/> を 1 度だけ呼び、ループは自分で止まります。
    /// </summary>
    protected CoroutineHandle MoveTo(Vector3 from, Vector3 to, float seconds, Action onArrive = null)
    {
        const float Step = 0.02f;

        float elapsed = 0f;
        CoroutineHandle handle = default;

        handle = Loop(Step, () =>
        {
            elapsed += Step;

            float progress = seconds <= 0f ? 1f : Mathf.Clamp01(elapsed / seconds);

            Position = Vector3.Lerp(from, to, progress);

            if (progress < 1f) return;

            Timing.KillCoroutines(handle);
            onArrive?.Invoke();
        });

        return handle;
    }

    /// <summary>
    /// Animator が指定ステートを再生し終わるのを待ってから実行します。
    /// Animator が無い / ステートへ入れない場合だけ <paramref name="fallbackSeconds"/> で代替します。
    /// </summary>
    protected CoroutineHandle WaitForAnimatorState(
        Animator animator,
        string stateName,
        float fallbackSeconds,
        Action action,
        float timeoutSeconds = 30f)
    {
        CoroutineHandle handle = RoundScope.Current.Track(Timing.RunCoroutine(
            AwaitAnimatorState(animator, stateName, Mathf.Max(0f, fallbackSeconds), Mathf.Max(0.5f, timeoutSeconds), action)));

        scheduled.Add(handle);

        return handle;
    }

    private IEnumerator<float> AwaitAnimatorState(
        Animator animator,
        string stateName,
        float fallbackSeconds,
        float timeoutSeconds,
        Action action)
    {
        const float PollInterval = 0.02f;
        const float EntryTimeout = 0.5f;

        float elapsed = 0f;
        bool entered = false;
        bool useFallback = animator == null || string.IsNullOrWhiteSpace(stateName);

        // Animator.Play の反映は次フレーム。
        yield return Timing.WaitForOneFrame;

        while (!useFallback && elapsed < timeoutSeconds)
        {
            yield return Timing.WaitForSeconds(PollInterval);
            elapsed += PollInterval;

            if (!IsAlive) yield break;

            if (animator == null)
            {
                useFallback = !entered;

                break;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.IsName(stateName))
            {
                entered = true;

                if (state.normalizedTime >= 1f && !animator.IsInTransition(0)) break;
            }
            else if (entered)
            {
                break;
            }
            else if (elapsed >= EntryTimeout)
            {
                useFallback = true;

                break;
            }
        }

        if (useFallback && fallbackSeconds > elapsed)
            yield return Timing.WaitForSeconds(fallbackSeconds - elapsed);

        if (IsAlive) action();
    }

    private void ApplyTransform()
    {
        // Create 前の代入は「初期値を置いただけ」。実体はまだ無いので通知もしない。
        if (!IsAlive) return;

        SyncTransform();
        OnTransformUpdated();
    }

    /// <summary>
    /// スキマティック内の <c>ObjectPrefabKey</c> 付きブロックを管理下へ取り込みます。
    /// Interactable を持つブロックはハンドルになり、その他はキーで引けるようになります。
    /// </summary>
    private void AdoptBlocks()
    {
        foreach (SchematicBlock block in Schematic.GetPrefabManagedBlocks())
        {
            string key = block.ObjectPrefabKey;
            managedBlocks[key] = block;

            if (!block.TryGetComponent(out InvisibleInteractableToy toyBase)) continue;

            InteractableHandle handle = new InteractableHandle(
                this,
                InteractableToy.Get(toyBase),
                Vector3.zero,
                Vector3.one,
                key,
                ownsToy: false,
                syncsTransform: false);

            interactables.Add(handle);
        }
    }

    private SchematicBlock FindBlock(string keyOrName)
    {
        if (GetBlock(keyOrName) is { } managed) return managed;

        return Schematic == null ? null : Schematic.FindBlock(keyOrName, allowPartial: false);
    }

    private void ClearInteractables(Func<InteractableHandle, bool> predicate, bool destroyToys)
    {
        for (int i = interactables.Count - 1; i >= 0; i--)
        {
            InteractableHandle handle = interactables[i];

            if (!predicate(handle)) continue;

            handle.Detach();
            interactables.RemoveAt(i);

            // 取り込み分 (OwnsToy=false) はスキマティックが寿命を持つので壊さない。
            if (!destroyToys || !handle.OwnsToy || handle.Toy.IsDestroyed) continue;

            handle.Toy.Destroy();
        }
    }

    private static void SetSubtreeSpawned(SchematicBlock block, bool spawned)
    {
        foreach (NetworkIdentity identity in block.GetComponentsInChildren<NetworkIdentity>(true))
        {
            if (identity == null || spawned == (identity.netId != 0)) continue;

            if (spawned)
                NetworkServer.Spawn(identity.gameObject);
            else
                NetworkServer.UnSpawn(identity.gameObject);
        }
    }
}
