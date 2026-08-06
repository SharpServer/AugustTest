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
using AntiMeme.Snake;

namespace AntiMeme.Audio;

public sealed class VideoFrameData
{
    public VideoFrameData(int index, TimeSpan timestamp, int width, int height, VideoPixelFormat pixelFormat, byte[] pixels) { Index = index; Timestamp = timestamp; Width = width; Height = height; PixelFormat = pixelFormat; Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels)); }
    public int Index { get; }
    public TimeSpan Timestamp { get; }
    public int Width { get; }
    public int Height { get; }
    public VideoPixelFormat PixelFormat { get; }
    public byte[] Pixels { get; }
    public byte GetGrayscale(int x, int y) { if (x < 0 || x >= Width || y < 0 || y >= Height) throw new ArgumentOutOfRangeException(); int offset = y * Width + x; return PixelFormat == VideoPixelFormat.Rgb24 ? (byte)Math.Min(255, (Pixels[offset * 3] * 299 + Pixels[offset * 3 + 1] * 587 + Pixels[offset * 3 + 2] * 114) / 1000) : Pixels[offset]; }
}
