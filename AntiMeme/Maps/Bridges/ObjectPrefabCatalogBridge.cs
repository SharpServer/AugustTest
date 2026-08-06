using System;
using System.Collections.Generic;
using System.Linq;
using ProjectMER.Features;
using ProjectMER.Features.Interfaces;
using Sliced.API.Enums;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Maps.Bridges;

/// <summary>
/// マップエディタに「置ける <see cref="ObjectPrefab"/> の一覧」と
/// 「各 Prefab が受け付けるオプション」を教えるブリッジです。
///
/// <para>
/// ProjectMER 側は AntiMeme を一切知りません (<see cref="IObjectPrefabInfoProvider"/> 越し)。
/// 一覧の出どころは <see cref="ObjectPrefab.KnownTypes"/> なので、
/// クラスを書き足すだけでエディタの候補に出ます。登録の呼び出しはどこにも書きません。
/// </para>
/// </summary>
public sealed class ObjectPrefabCatalogBridge : EventHandlerBase, IObjectPrefabInfoProvider
{
    /// <inheritdoc/>
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <inheritdoc/>
    public IReadOnlyList<ObjectPrefabTypeInfo> GetPrefabTypes()
        => ObjectPrefab.KnownTypes
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .Select(type => new ObjectPrefabTypeInfo
            {
                Key = type.Name,
                DisplayName = type.Name,
                Aliases = new[] { type.FullName },
            })
            .ToArray();

    /// <inheritdoc/>
    public bool TryGetOptionDefinitions(
        string prefabTypeName,
        out IReadOnlyList<ObjectPrefabOptionInfo> definitions,
        out string error)
    {
        definitions = Array.Empty<ObjectPrefabOptionInfo>();

        if (ObjectPrefab.FindType(prefabTypeName) is not { } prefabType)
        {
            error = $"ObjectPrefab '{prefabTypeName}' が見つかりません。";

            return false;
        }

        // 既定値を読むためだけの一時インスタンス。Create() はしないので何もスポーンしない。
        try
        {
            definitions = ObjectPrefabOptions.Describe((ObjectPrefab)Activator.CreateInstance(prefabType));
            error = string.Empty;

            return true;
        }
        catch (Exception exception)
        {
            error = $"{prefabType.Name} を生成できませんでした: {exception.Message}";
            Logger.Error($"[ObjectPrefabCatalogBridge] {error}");

            return false;
        }
    }

    /// <inheritdoc/>
    public override void RegisterEvents() => ObjectPrefabInfoRegistry.Provider = this;

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        if (ReferenceEquals(ObjectPrefabInfoRegistry.Provider, this))
            ObjectPrefabInfoRegistry.Provider = null;
    }
}
