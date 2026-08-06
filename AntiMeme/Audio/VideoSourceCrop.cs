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

public sealed class VideoSourceCrop
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 1f;
    public float Height { get; set; } = 1f;
    internal VideoSourceCrop Snapshot() { if (X < 0 || Y < 0 || Width <= 0 || Height <= 0 || X + Width > 1.00001f || Y + Height > 1.00001f) throw new ArgumentOutOfRangeException(nameof(VideoSourceCrop)); var copy = (VideoSourceCrop)MemberwiseClone(); copy.Width = Math.Min(copy.Width, 1f - copy.X); copy.Height = Math.Min(copy.Height, 1f - copy.Y); return copy; }
}
