using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features.Warhead;

public sealed class OmegaWarheadStartingEventArgs : EventArgs
{
    public OmegaWarheadStartingEventArgs(Player player, bool isAllowed) { Player = player; IsAllowed = isAllowed; }
    public Player Player { get; }
    public bool IsAllowed { get; set; }
}
