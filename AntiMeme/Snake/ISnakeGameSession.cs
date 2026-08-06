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

public interface ISnakeGameSession : IDisposable { ushort Serial { get; } int PlayerId { get; } bool IsRunning { get; } void Start(); void HandleInput(Vector2Int direction); void Stop(bool restoreSnake); }
