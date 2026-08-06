using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntiMeme.Maps;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// 宣言されているものを型名で列挙します。ここに出る名前が、そのまま
/// <c>am spawn</c> / <c>am give</c> / <c>am ability</c> / <c>am mode</c> の引数になります。
/// </summary>
public sealed class ListCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "list";

    public override string Usage => "list <roles|items|abilities|teams|modes|prefabs> [絞り込み]";

    public override string Description => "宣言済みの役職・アイテム・アビリティ・チーム・ゲームモード・Prefab を列挙します。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string category))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        TryGetArgument(1, out string filter);

        switch (category.ToLowerInvariant())
        {
            case "roles":
            case "role":
                response = Render("役職", TypeNames<CustomRole>(), filter);

                return true;

            case "items":
            case "item":
                response = Render("アイテム", TypeNames<CustomItem>(), filter);

                return true;

            case "abilities":
            case "ability":
                response = Render("アビリティ", TypeNames<AbilityBase>(), filter);

                return true;

            case "prefabs":
            case "prefab":
                response = Render("ObjectPrefab", ObjectPrefab.KnownTypes.Select(type => type.Name), filter);

                return true;

            case "teams":
            case "team":
                response = Render(
                    "チーム",
                    CustomTeam.All.Select(team => $"{team.GetType().Name} ({team.Name})"),
                    filter);

                return true;

            case "modes":
            case "mode":
                response = Render(
                    "ゲームモード",
                    GameMode.All().Select(mode =>
                        $"{mode.GetType().Name} ({mode.Name}) 重み {mode.Weight} / 最小 {mode.MinimumPlayers} 人"),
                    filter);

                return true;

            default:
                response = $"'{category}' は知らない区分です。使い方: am {Usage}";

                return false;
        }
    }

    private static IEnumerable<string> TypeNames<TBase>() =>
        TypeParser.FindTypes<TBase>().Select(type => type.Name);

    private static string Render(string title, IEnumerable<string> entries, string filter)
    {
        List<string> matched = entries
            .Where(entry => string.IsNullOrWhiteSpace(filter) ||
                            entry.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matched.Count == 0)
            return $"{title}: 該当なし。";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"<b>{title}</b>  ({matched.Count} 件)");

        foreach (string entry in matched)
            builder.AppendLine($"  {entry}");

        return builder.ToString().TrimEnd();
    }
}
