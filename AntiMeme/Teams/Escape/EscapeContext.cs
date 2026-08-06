using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps;
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

namespace AntiMeme.Teams.Escape;

/// <summary>
/// 脱出前のプレイヤー状態です。役職と拘束者が名乗るチームを一度だけ解決します。
/// </summary>
public sealed class EscapeContext
{
    public EscapeContext(Player player)
    {
        Player = player;
        PlayerTeam = FindTeam(player);
        CufferTeam = FindTeam(player?.DisarmedBy);
    }

    public Player Player { get; }

    public CustomTeam PlayerTeam { get; }

    public CustomTeam CufferTeam { get; }

    private static CustomTeam FindTeam(Player player) => player is null
        ? null
        : CustomTeam.All.FirstOrDefault(team => team.Includes(player));
}
