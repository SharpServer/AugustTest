using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CentralAuth;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using HarmonyLib;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Keycards.Snake;
using MEC;
using Mirror;
using SNAPI.Events.EventArgs;
using SNAPI.Events.Handlers;
using UnityEngine;

using ExiledPlayer = Exiled.API.Features.Player;
using AntiMeme.Audio;

namespace AntiMeme.Snake;

public sealed class SnakeMediaOptions
{
    public SnakeImageOptions Image { get; set; } = new();
    public bool IsSpatial { get; set; } = true;
    public bool FollowOwner { get; set; } = true;
    public float MaxDistance { get; set; } = 12f;
    public float MinDistance { get; set; } = 1f;
    public float Volume { get; set; } = 1f;
    public string AudioPlayerName { get; set; }
    public Predicate<ExiledPlayer> Listeners { get; set; }
}
