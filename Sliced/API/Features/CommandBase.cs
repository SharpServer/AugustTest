using System;
using System.Linq;
using System.Reflection;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using Sliced.API.Extensions;

namespace Sliced.API.Features;

public abstract class CommandBase : ICommand
{
    public abstract string Command { get; }
    public virtual string[] Aliases { get; } = [];
    public virtual string Description { get; } = string.Empty;

    protected Player Sender { get; private set; }
    protected ArraySegment<string> Arguments { get; private set; }
    protected ICommandSender InterfaceSender { get; private set; }
    
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Sender = Player.Get(sender);
        Arguments = arguments;
        InterfaceSender = sender;
        Assembly assembly = Assembly.GetExecutingAssembly();
        if (!sender.HasPermission($"{assembly.GetName()}.{Command}"))
        {
            response = "にゃ～、てめえは権限持ってないにゃ～！";
            return false;
        }

        bool isSuccess = OnExecute(out string res);
        response = res;
        return isSuccess;
    }

    protected abstract bool OnExecute(out string response);
}