using System;
using System.Collections.Generic;
using AdminToys;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps;

/// <summary>
/// <see cref="ObjectPrefab"/> が管理する Interactable 1 個ぶんのハンドルです。
///
/// <para>
/// <see cref="Interacting"/> / <see cref="Interacted"/> は、この Interactable が
/// 操作対象になったときだけ発火します。Toy の実体からハンドルを引く表を持っているので、
/// 座標の近さで当てるようなあいまいな照合はしません。
/// </para>
/// </summary>
public sealed class InteractableHandle
{
    private static readonly Dictionary<InvisibleInteractableToy, InteractableHandle> Routes =
        new Dictionary<InvisibleInteractableToy, InteractableHandle>();

    internal InteractableHandle(
        ObjectPrefab owner,
        InteractableToy toy,
        Vector3 localOffset,
        Vector3 baseScale,
        string key = "",
        bool ownsToy = true,
        bool syncsTransform = true)
    {
        Owner = owner;
        Toy = toy;
        LocalOffset = localOffset;
        BaseScale = baseScale;
        Key = key;
        OwnsToy = ownsToy;
        SyncsTransform = syncsTransform;

        Routes[toy.Base] = this;
    }

    /// <summary>
    /// この Interactable を持っている Prefab です。
    /// </summary>
    public ObjectPrefab Owner { get; }

    /// <summary>
    /// Interactable の実体です。
    /// </summary>
    public InteractableToy Toy { get; }

    /// <summary>
    /// スキマティックから取り込んだ場合の <c>ObjectPrefabKey</c> です。
    /// <see cref="ObjectPrefab.AddInteractable"/> で作ったものは空文字。
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Prefab が Toy の寿命を持っているかどうか。取り込み分はスキマティック側が持つので false。
    /// </summary>
    public bool OwnsToy { get; }

    /// <summary>
    /// Prefab の transform に追従させるかどうか。取り込み分は親子関係で追従済みなので false。
    /// </summary>
    public bool SyncsTransform { get; }

    /// <summary>
    /// Prefab 原点からのローカルオフセットです。
    /// </summary>
    public Vector3 LocalOffset { get; set; }

    /// <summary>
    /// Prefab の <see cref="ObjectPrefab.Scale"/> と掛け合わされる基本スケールです。
    /// </summary>
    public Vector3 BaseScale { get; set; }

    /// <summary>
    /// false にすると操作が自動でキャンセルされます。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 長押しが始まったときに発火します。<c>ev.IsAllowed = false</c> で止められます。
    /// </summary>
    public event Action<PlayerSearchingToyEventArgs> Interacting;

    /// <summary>
    /// 操作が完了したときに発火します。長押し時間 0 の即時操作も含みます。
    /// </summary>
    public event Action<PlayerSearchedToyEventArgs> Interacted;

    /// <summary>
    /// Toy の実体から対応するハンドルを引きます。
    /// </summary>
    public static bool TryRoute(InteractableToy toy, out InteractableHandle handle)
    {
        handle = null;

        return toy != null && Routes.TryGetValue(toy.Base, out handle);
    }

    internal void Detach() => Routes.Remove(Toy.Base);

    internal void RaiseInteracting(PlayerSearchingToyEventArgs ev)
    {
        if (!Enabled)
        {
            ev.IsAllowed = false;

            return;
        }

        Interacting?.Invoke(ev);
    }

    internal void RaiseInteracted(PlayerSearchedToyEventArgs ev) => Interacted?.Invoke(ev);
}
