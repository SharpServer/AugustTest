using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.Items.Pickups;
using LabApi.Features.Wrappers;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using Sliced.API.Enums;
using Sliced.API.Features;
using Sliced.API.Features.Attributes;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Maps.Bridges;

/// <summary>
/// ProjectMER の ItemSpawnpoint に置かれた「カスタムアイテム名」を
/// <see cref="CustomItem"/> として解決するブリッジです。
///
/// <para>
/// マップ側に書かれるのは型名なので、<see cref="TypeParser"/> がそのまま解決します。
/// アイテムのキー表は作りません。
/// </para>
/// </summary>
public sealed class SpawnpointItemBridge : EventHandlerBase
{
    private static readonly Dictionary<string, Type> ResolvedTypes =
        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <inheritdoc/>
    public override void RegisterEvents() => ItemSpawnpointCustomItemRegistry.Register(TrySpawn, TryGive);

    /// <inheritdoc/>
    public override void UnregisterEvents() => ItemSpawnpointCustomItemRegistry.Unregister(TrySpawn, TryGive);

    private static bool TrySpawn(
        string customItemKey,
        SerializableItemSpawnpoint spawnpoint,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        out ItemPickupBase pickupBase)
    {
        pickupBase = null;

        if (FindType(customItemKey) is not { } itemType) return false;

        if (CustomItem.Spawn(itemType, position, rotation, spawnpoint.Scale) is not { } custom)
            return false;

        if (custom.Pickup is not { } pickup)
        {
            custom.Destroy();

            return false;
        }

        pickup.Transform.parent = parent;
        pickup.IsLocked = !spawnpoint.CanBePickedUp;

        if (spawnpoint.Weight >= 0f)
            pickup.Weight = spawnpoint.Weight;

        if (pickup.Rigidbody is { } rigidbody)
            rigidbody.isKinematic = !spawnpoint.UseGravity;

        pickupBase = pickup.Base;

        return true;
    }

    private static bool TryGive(string customItemKey, ItemPickupBase pickup, Player player)
        => FindType(customItemKey) is { } itemType && CustomItem.Give(itemType, player) is not null;

    /// <summary>
    /// 型名から <see cref="CustomItem"/> の具象型を引きます。
    /// 生成には引数なしコンストラクタが要るので、持たない型は弾きます。
    /// </summary>
    /// <remarks>
    /// 引けなかったキーは 1 度だけ警告に出します。ProjectMER 側は
    /// 「has no provider」としか言わないので、マップに書かれた綴りと
    /// 実在する型のどちらが悪いのかがログから分かりません。
    /// 改名した型は <see cref="LegacyNameAttribute"/> で昔の綴りを名乗らせてください。
    /// </remarks>
    private static Type FindType(string customItemKey)
    {
        if (string.IsNullOrWhiteSpace(customItemKey)) return null;

        string key = customItemKey.Trim();

        if (ResolvedTypes.TryGetValue(key, out Type cached)) return cached;

        TypeParser.TryParse<CustomItem>(key, out Type resolved);

        if (resolved is not null && resolved.GetConstructor(Type.EmptyTypes) is null)
        {
            Logger.Warn(
                $"[AntiMeme] マップのカスタムアイテム '{key}' は {resolved.FullName} に解決できましたが、" +
                "引数なしコンストラクタが無いため生成できません。");

            resolved = null;
        }
        else if (resolved is null)
        {
            Logger.Warn(
                $"[AntiMeme] マップのカスタムアイテム '{key}' に対応する型がありません。" +
                $"綴りの近いもの: {Suggest(key)}");
        }

        ResolvedTypes[key] = resolved;

        return resolved;
    }

    /// <summary>綴り間違いを見つけやすくするため、前方一致するアイテム名を挙げます。</summary>
    private static string Suggest(string key)
    {
        string prefix = key.Length <= 4 ? key : key.Substring(0, 4);

        string[] near = TypeParser.FindTypes<CustomItem>()
            .Select(type => type.Name)
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .Take(5)
            .ToArray();

        return near.Length == 0 ? "なし" : string.Join(", ", near);
    }
}
