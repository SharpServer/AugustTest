using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;

namespace AntiMeme.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class RootCommand : ParentCommand
{
    public RootCommand() => LoadGeneratedCommands();
    public override string Command => "am";
    public override string[] Aliases => [];
    public override string Description => "AntiMeme Plugin's Root Command";
    
    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new RoleCommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "にゃー";
        return true;
    }
}