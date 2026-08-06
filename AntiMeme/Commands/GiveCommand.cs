using System;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// カスタムアイテムを渡します。アイテムはクラス名で指定します (例: <c>am give Mindblaster</c>)。
/// バニラの <see cref="ItemType"/> 名も同じ引数で受け付けます。
/// </summary>
public sealed class GiveCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "give";

    public override string Usage => "give <アイテムクラス名 | ItemType> [対象]";

    public override string Description => "カスタムアイテムまたはバニラアイテムを渡します。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string itemName))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        if (!TryGetPlayer(1, out Player target))
        {
            response = "対象のプレイヤーが見つかりませんでした。サーバーコンソールからは対象を明示してください。";

            return false;
        }

        if (TypeParser.TryParse<CustomItem>(itemName, out Type itemType))
        {
            if (CustomItem.Give(itemType, target) is not { } given)
            {
                response = $"{itemType.Name} を渡せませんでした (インベントリが満杯の可能性があります)。";

                return false;
            }

            response = $"{target.Nickname} に {given.Name} を渡しました。";

            return true;
        }

        if (Enum.TryParse(itemName, ignoreCase: true, out ItemType vanilla) && vanilla is not ItemType.None)
        {
            target.AddItem(vanilla);
            response = $"{target.Nickname} に {vanilla} を渡しました。";

            return true;
        }

        response = $"'{itemName}' というアイテムは見つかりませんでした。 (am list items で一覧)";

        return false;
    }
}
