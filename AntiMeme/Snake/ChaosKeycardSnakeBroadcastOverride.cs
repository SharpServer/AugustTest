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

[HarmonyPatch(typeof(ChaosKeycardItem), nameof(ChaosKeycardItem.ServerSendMessage), typeof(SnakeNetworkMessage))]
internal static class ChaosKeycardSnakeBroadcastOverride
{
    private static bool Prefix(ChaosKeycardItem __instance, SnakeNetworkMessage msg) => !SnakeImageApi.BlocksClientSnakeSync(__instance.ItemSerial) || !msg.HasFlag(SnakeNetworkMessage.SyncFlags.Delta);
}
