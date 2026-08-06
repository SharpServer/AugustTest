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

public sealed class SnakeImagePlayback : IDisposable
{
    private readonly ChaosKeycardItem keycard;
    private readonly IReadOnlyList<List<Vector2Int>> frames;
    private readonly SnakeImageOptions options;
    private CoroutineHandle coroutine;
    private bool stopped;
    internal SnakeImagePlayback(ChaosKeycardItem keycard, IReadOnlyList<List<Vector2Int>> frames, SnakeImageOptions options) { this.keycard = keycard; this.frames = frames; this.options = options; Serial = keycard.ItemSerial; }
    public ushort Serial { get; }
    public int FrameCount => frames.Count;
    public bool IsPlaying => !stopped;
    internal bool StopsOnSnakeInput => options.StopOnSnakeInput;
    internal bool BlocksClientSnakeSync => !stopped && options.TakeOverOwnerSession;
    internal void Start() => coroutine = Timing.RunCoroutine(Run());
    public void Stop() => Stop(options.RestoreSnakeOnStop, true);
    public void Dispose() => Stop();
    internal void Stop(bool restoreSnake, bool killCoroutine)
    { if (stopped) return; stopped = true; if (killCoroutine) Timing.KillCoroutines(coroutine); if (restoreSnake) Restore(); SnakeImageApi.Remove(this); }
    private IEnumerator<float> Run()
    {
        if (frames.Count == 0) { Stop(false, false); yield break; }
        float delay = 1f / options.FramesPerSecond; int index = 0;
        while (!stopped && Valid()) { if (!Send(frames[index])) break; index++; if (index >= frames.Count) { if (!options.Loop) break; index = 0; } yield return Timing.WaitForSeconds(delay); }
        Stop(options.RestoreSnakeOnStop, false);
    }
    private bool Send(List<Vector2Int> frame) { if (!Valid()) return false; try { keycard.ServerSendMessage(SnakeNetworkMessage.NewFullResync(false, frame, null)); return true; } catch { return false; } }
    private bool Valid() { Item item = Item.Get(Serial); if (item is not Keycard card || card.Base != keycard) return false; return !options.StopWhenUnequipped || card.Owner?.CurrentItem?.Serial == Serial; }
    private void Restore() { try { var context = SNAPI.Features.SnakeContext.Get(Serial); List<Vector2Int> segments = context?.Segments?.Count >= 2 ? new List<Vector2Int>(context.Segments) : SnakeImageApi.DefaultSegments(); keycard.ServerSendMessage(SnakeNetworkMessage.NewFullResync(false, segments, context?.NextFoodPosition)); } catch { } }
}
