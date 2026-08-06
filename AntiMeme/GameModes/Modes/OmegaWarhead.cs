using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Hud;
using AntiMeme.Maps;
using AntiMeme.Maps.Features;
using AntiMeme.Maps.Features.Warhead;
using AntiMeme.Maps.Objects;
using AntiMeme.Roles;
using AntiMeme.Spawning;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using MEC;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using ExiledRoom = Exiled.API.Features.Room;
using SystemRandom = System.Random;

namespace AntiMeme.GameModes.Modes;

public sealed class OmegaWarhead : RaidMode
{
    public override string Name => "OMEGA WARHEAD";
    public override string Description => "OMEGA WARHEADが収容違反を終結させる。";
    public override int MinimumPlayers => 6;
    public override int Weight => 5;
    public override bool AllowsWarhead => false;

    protected override void OnStarted()
    {
        Say("O5 Command has decided to halt containment breaches using alpha warhead", "O5評議会がALPHA WARHEADによる収容違反の一時解決を決定しました。", true);
        Delay(60f, () => Say("New Status Accepted. Using OMEGA WARHEAD", "対応ステータスをOMEGA WARHEADへ更新します。", true));
        Delay(120f, () => AntiMeme.Maps.Features.Warhead.OmegaWarhead.StartProtocol());
    }

    protected override void OnStopped() 
    {
        AntiMeme.Maps.Features.Warhead.OmegaWarhead.Reset();
        base.OnStopped();
    }
}
