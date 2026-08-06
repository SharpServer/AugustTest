using System;
using System.Linq;
using System.Text;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// 1 つのコマンドの使い方と必要な権限ノードを表示します。
/// 一覧は <c>am</c> を引数なしで実行すると出ます。
/// </summary>
public sealed class HelpCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "help";

    public override string Usage => "help <コマンド名>";

    public override string Description => "コマンドの使い方と必要な権限を表示します。";

    // 使い方の確認自体に権限を要求しない。実行できるかは各コマンドが自分で見る。
    public override string Permission => string.Empty;

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string commandName))
        {
            response = "使い方: am help <コマンド名>  (一覧は引数なしの am)";

            return false;
        }

        CommandBase target = Siblings()
            .FirstOrDefault(command =>
                string.Equals(command.Command, commandName, StringComparison.OrdinalIgnoreCase) ||
                command.Aliases.Any(alias => string.Equals(alias, commandName, StringComparison.OrdinalIgnoreCase)));

        if (target is null)
        {
            response = $"'{commandName}' というコマンドはありません。";

            return false;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"<b>am {target.Usage}</b>");

        if (target.Aliases is { Length: > 0 })
            builder.AppendLine($"  別名   : {string.Join(", ", target.Aliases)}");

        if (target.Description is { Length: > 0 })
            builder.AppendLine($"  説明   : {target.Description}");

        builder.AppendLine($"  権限   : {(target.Permission is { Length: > 0 } node ? node : "不要")}");
        builder.AppendLine($"  実行可否: {(target.IsAllowedFor(InterfaceSender) ? "可" : "不可")}");

        response = builder.ToString().TrimEnd();

        return true;
    }

    /// <summary>
    /// 同じ親を持つコマンドを集めます。親側の登録一覧に触らずに済ませるためです。
    /// </summary>
    private static CommandBase[] Siblings() => TypeParser.FindTypes<CommandBase>()
        .Select(type => (CommandBase)Activator.CreateInstance(type))
        .Where(command => command.Parent == typeof(RootCommand))
        .ToArray();
}
