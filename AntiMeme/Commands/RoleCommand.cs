using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using LabApi.Features.Permissions;
using Sliced.API.Features;

namespace AntiMeme.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class RoleCommand : CommandBase
{
    public override string Command => "role";
    public override string[] Aliases => [];
    public override string Description => "bruh";

    protected override bool OnExecute(out string response)
    {
        if (TypeParser.TryCreate(Arguments[0], out CustomRole role))
        {
            if (role is null)
            {
                response = "Something went wrong! Please, try again later!";
                return false;
            }
            if (role.Spawn(Sender))
            {
                response = $"Role {role.Name} was spawned Successfully!";
                return true;
            }
            response = "Something went wrong! Please try again later!";
            return false;
        }
        response = "Role not found! Please check correctly Role Name!";
        return false;
    }
}