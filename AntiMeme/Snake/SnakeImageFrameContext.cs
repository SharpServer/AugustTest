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

public sealed class SnakeImageFrameContext
{
    internal SnakeImageFrameContext(VideoFrameData frame, SnakeImageOptions options) { Frame = frame; Options = options; }
    public VideoFrameData Frame { get; }
    public SnakeImageOptions Options { get; }
    public bool IsForeground(int x, int y) { int sourceY = Options.FlipVertically ? Options.Height - 1 - y : y; bool foreground = Frame.GetGrayscale(x, sourceY) >= Options.Threshold; return Options.Invert ? !foreground : foreground; }
    public Vector2Int ToDisplayPosition(int x, int y) => new(Options.OffsetX + x, Options.OffsetY + y);
}
