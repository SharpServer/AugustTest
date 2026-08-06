using CommandSystem;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// AntiMeme のすべての運営コマンドの入口です。
///
/// 子コマンドは <see cref="CommandBase.Parent"/> に <c>typeof(RootCommand)</c> を書くだけで並びます。
/// ここに登録の一覧を書いてはいけません
/// (旧 Slafight の RootCommand が 30 行の RegisterCommand を抱えていたのがこれで消えています)。
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class RootCommand : ParentCommandBase
{
    public override string Command => "am";

    public override string[] Aliases { get; } = ["antimeme"];

    public override string Description => "AntiMeme の運営コマンド。";

    protected override string Title => "AntiMeme Commands";
}
