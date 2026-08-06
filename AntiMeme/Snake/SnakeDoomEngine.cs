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

public sealed class SnakeDoomEngine : ISnakeGameSession
{
    private readonly ExiledPlayer player; private readonly Action<SnakeDoomEngine> stopped; private bool running; private CoroutineHandle loop; private int health = 100; private int ammo = 50; private int kills;
    public SnakeDoomEngine(ExiledPlayer player, ushort serial, Action<SnakeDoomEngine> onStopped = null) { this.player = player ?? throw new ArgumentNullException(nameof(player)); Serial = serial; stopped = onStopped; kills = 0; if (!Valid()) throw new InvalidOperationException("The serial is not an owned Chaos Keycard."); }
    public ushort Serial { get; } public int PlayerId => player.Id; public bool IsRunning => running; public int Health => health; public int Ammo => ammo; public int Kills => kills;
    public void Start() { if (running) return; running = true; loop = Timing.RunCoroutine(Loop()); }
    public void HandleInput(Vector2Int direction) { if (!running) return; if (direction == Vector2Int.up && ammo > 0) ammo--; }
    public void Stop() => Stop(true); public void Stop(bool restoreSnake) { if (!running) return; running = false; Timing.KillCoroutines(loop); SnakeImageApi.Stop(Serial); stopped?.Invoke(this); }
    public void Dispose() => Stop(false);
    private IEnumerator<float> Loop() { while (running && Valid()) yield return Timing.WaitForSeconds(.1f); if (running) Stop(false); }
    private bool Valid() => player.CurrentItem is Keycard card && card.Serial == Serial && card.Base is ChaosKeycardItem;
}
