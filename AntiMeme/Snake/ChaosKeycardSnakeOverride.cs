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

[HarmonyPatch(typeof(ChaosKeycardItem), nameof(ChaosKeycardItem.ServerProcessCustomCmd), typeof(NetworkReader))]
internal static class ChaosKeycardSnakeOverride
{
    private static bool Prefix(ChaosKeycardItem __instance, NetworkReader reader)
    { if (__instance == null || reader == null || !SnakeImageApi.BlocksClientSnakeSync(__instance.ItemSerial)) return true; int position = reader.Position; try { return reader.Remaining == 0 || (ChaosKeycardItem.ChaosMsgType)reader.ReadByte() != ChaosKeycardItem.ChaosMsgType.SnakeMsgSync; } finally { reader.Position = position; } }
}
