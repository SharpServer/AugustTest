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

public sealed class SnakeImageOptions
{
    public const int NativeWidth = 18;
    public const int NativeHeight = 11;
    public int Width { get; set; } = NativeWidth;
    public int Height { get; set; } = NativeHeight;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public float FramesPerSecond { get; set; } = 10f;
    public int MaxFrames { get; set; } = 300;
    public float? TimelineDurationSeconds { get; set; }
    public byte Threshold { get; set; } = 128;
    public bool Invert { get; set; }
    public bool FlipVertically { get; set; } = true;
    public SnakeImageRenderStyle RenderStyle { get; set; } = SnakeImageRenderStyle.NativeSnake;
    public VideoSourceCrop SourceCrop { get; set; }
    public int AbstractionLevel { get; set; } = 2;
    public ISnakeImageFrameRenderer CustomRenderer { get; set; }
    public bool RenderSolidPixels { get; set; }
    public bool Loop { get; set; } = true;
    public bool RestoreSnakeOnStop { get; set; } = true;
    public bool StopWhenUnequipped { get; set; } = true;
    public bool StopOnSnakeInput { get; set; } = true;
    public bool TakeOverOwnerSession { get; set; }
    public bool UseYtDlp { get; set; }
    internal SnakeImageOptions Snapshot()
    { if (Width < 1 || Width > NativeWidth || Height < 1 || Height > NativeHeight || OffsetX < 0 || OffsetY < 0 || OffsetX + Width > NativeWidth || OffsetY + Height > NativeHeight || FramesPerSecond <= 0 || FramesPerSecond > 30 || MaxFrames < 1 || MaxFrames > 10000 || AbstractionLevel < 0 || AbstractionLevel > 3) throw new ArgumentOutOfRangeException(nameof(SnakeImageOptions)); if (TimelineDurationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(TimelineDurationSeconds)); var copy = (SnakeImageOptions)MemberwiseClone(); copy.SourceCrop = SourceCrop?.Snapshot(); return copy; }
}
