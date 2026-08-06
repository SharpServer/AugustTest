using System;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using AntiMeme.Roles.Scps;

namespace AntiMeme.Commands;

/// <summary>
/// カスタム役職を付与します。役職はクラス名で指定します (例: <c>am spawn Scp3005</c>)。
/// 相手を省略すると自分に付与します。
/// </summary>
public sealed class SpawnCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "spawn";

    public override string[] Aliases { get; } = ["role"];

    public override string Usage => "spawn <役職クラス名> [対象]";

    public override string Description => "カスタム役職を付与します。対象を省略すると自分に付与します。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string roleName))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        if (!TryGetPlayer(1, out Player target))
        {
            response = "対象のプレイヤーが見つかりませんでした。サーバーコンソールからは対象を明示してください。";

            return false;
        }

        if (!TypeParser.TryCreate(roleName, out CustomRole role))
        {
            response = $"'{roleName}' という役職は見つかりませんでした。 (am list roles で一覧)";

            return false;
        }

        if (!role.Spawn(target, out string failure))
        {
            response = $"{role.Name} のスポーンに失敗しました。{failure}";

            return false;
        }

        response = $"{target.Nickname} を {role.Name} にしました。";

        return true;
    }
}
