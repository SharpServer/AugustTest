using System;
using System.Linq;
using System.Text;
using AntiMeme.Maps;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Commands;

/// <summary>
/// ObjectPrefab をその場に出したり、今出ているものを数えたりします。
/// マップの本番配置は ProjectMER の <c>object_prefab_markers:</c> が持つので、
/// これは配置前の当たりを付けるための道具です。
/// </summary>
public sealed class PrefabCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "prefab";

    public override string Usage => "prefab <spawn|list|clear> [型名] [名札]";

    public override string Description => "ObjectPrefab を足元に生成・一覧・破棄します。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string action))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        switch (action.ToLowerInvariant())
        {
            case "spawn":
                return Spawn(out response);

            case "list":
                response = List();

                return true;

            case "clear":
                return Clear(out response);

            default:
                response = $"'{action}' は知らない操作です。使い方: am {Usage}";

                return false;
        }
    }

    private bool Spawn(out string response)
    {
        if (Sender is null)
        {
            response = "生成位置を決められないため、ゲーム内から実行してください。";

            return false;
        }

        if (!TryGetArgument(1, out string typeName))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        if (ObjectPrefab.FindType(typeName) is not { } prefabType)
        {
            response = $"'{typeName}' という ObjectPrefab は見つかりませんでした。 (am list prefabs で一覧)";

            return false;
        }

        ObjectPrefab prefab = (ObjectPrefab)Activator.CreateInstance(prefabType);
        prefab.Position = Sender.Position;
        prefab.Rotation = Quaternion.Euler(0f, Sender.Rotation.eulerAngles.y, 0f);
        prefab.IsSaveable = false;

        if (TryGetArgument(2, out string tag))
            prefab.Tag = tag;

        prefab.Create();

        response = $"{prefabType.Name} を {prefab.Position} に生成しました。";

        return true;
    }

    private static string List()
    {
        if (ObjectPrefab.All.Count == 0)
            return "生きている ObjectPrefab はありません。";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"<b>ObjectPrefab</b>  ({ObjectPrefab.All.Count} 個)");

        foreach (var group in ObjectPrefab.All
                     .GroupBy(prefab => prefab.GetType().Name)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            string tags = string.Join(", ", group
                .Select(prefab => prefab.Tag)
                .Where(tag => tag.Length > 0)
                .Distinct());

            builder.AppendLine($"  {group.Key} x{group.Count()}{(tags.Length > 0 ? $"  [{tags}]" : string.Empty)}");
        }

        return builder.ToString().TrimEnd();
    }

    private bool Clear(out string response)
    {
        TryGetArgument(1, out string filter);

        ObjectPrefab[] targets = ObjectPrefab.All
            .Where(prefab => string.IsNullOrWhiteSpace(filter) ||
                             string.Equals(prefab.GetType().Name, filter, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(prefab.Tag, filter, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (ObjectPrefab prefab in targets)
            prefab.Destroy();

        response = $"{targets.Length} 個の ObjectPrefab を破棄しました。";

        return true;
    }
}
