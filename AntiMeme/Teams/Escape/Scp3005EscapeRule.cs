using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps;
using AntiMeme.Roles.Scps;
using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;
using AntiMeme.Roles.Fifthist;

namespace AntiMeme.Teams.Escape;

/// <summary>SCP-3005 は脱出すると第五教会の司祭になります。</summary>
public sealed class Scp3005EscapeRule : EscapeRule
{
    public override int Priority => 0;

    public override EscapeTarget Resolve(EscapeContext context) =>
        CustomRole.Is<Scp3005>(context.Player)
            ? EscapeTarget.CustomRole<FifthistPriest>()
            : EscapeTarget.None;
}
