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

public static class SnakeImageApi
{
    private static readonly Dictionary<ushort, SnakeImagePlayback> ActivePlaybacks = new();
    private static bool registered;
    public static IReadOnlyCollection<SnakeImagePlayback> Active => ActivePlaybacks.Values.ToArray();
    internal static void RegisterEvents() { if (registered) return; SnakePlayer.SnakeMove += OnSnakeMove; Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers; registered = true; }
    internal static void UnregisterEvents() { if (!registered) return; SnakePlayer.SnakeMove -= OnSnakeMove; Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers; StopAll(false); registered = false; }
    public static SnakeImagePlayback Play(ExiledPlayer player, string location, SnakeImageOptions options = null) { Keycard card = player?.CurrentItem as Keycard; if (card?.Base is not ChaosKeycardItem) throw new InvalidOperationException("Player must hold a Chaos Keycard."); return Play(card, location, options); }
    public static SnakeImagePlayback Play(Keycard keycard, string location, SnakeImageOptions options = null) => Play(keycard?.Serial ?? throw new ArgumentNullException(nameof(keycard)), location, options);
    public static SnakeImagePlayback Play(ushort serial, string location, SnakeImageOptions options = null)
    { if (File.Exists(location)) return PlayFile(serial, location, options); if (YtDlp.IsUrl(location)) return PlayUrl(serial, location, options); throw new FileNotFoundException("Snake media location was not found.", location); }
    public static SnakeImagePlayback PlayFile(ExiledPlayer player, string path, SnakeImageOptions options = null) => Play(player, path, options);
    public static SnakeImagePlayback PlayFile(Keycard keycard, string path, SnakeImageOptions options = null) => Play(keycard, path, options);
    public static SnakeImagePlayback PlayFile(ushort serial, string path, SnakeImageOptions options = null)
    { options = (options ?? new SnakeImageOptions()).Snapshot(); IReadOnlyList<byte[]> raw = Ffmpeg.DecodeFrames(path, options.Width, options.Height, options.FramesPerSecond, options.MaxFrames); return PlayFrames(serial, raw.Select((x, i) => new VideoFrameData(i, TimeSpan.FromSeconds(i / options.FramesPerSecond), options.Width, options.Height, VideoPixelFormat.Grayscale8, x)).ToArray(), options); }
    public static SnakeImagePlayback PlayUrl(ExiledPlayer player, string url, SnakeImageOptions options = null) => Play(player, url, options);
    public static SnakeImagePlayback PlayUrl(Keycard keycard, string url, SnakeImageOptions options = null) => Play(keycard?.Serial ?? 0, url, options);
    public static SnakeImagePlayback PlayUrl(ushort serial, string url, SnakeImageOptions options = null)
    { string path = options?.UseYtDlp == true ? YtDlp.Download(url) : url; if (path == null) return null; try { return PlayFile(serial, path, options); } finally { if (options?.UseYtDlp == true) YtDlp.Discard(path); } }
    public static SnakeImagePlayback PlayFrames(ushort serial, IReadOnlyList<VideoFrameData> frames, SnakeImageOptions options = null)
    { Item item = Item.Get(serial); if (item is not Keycard card || card.Base is not ChaosKeycardItem keycard) throw new InvalidOperationException("The serial is not an owned Chaos Keycard."); options = (options ?? new SnakeImageOptions()).Snapshot(); var positions = frames?.Select(frame => CreateSegments(frame, options)).Where(x => x.Count > 0).ToArray() ?? Array.Empty<List<Vector2Int>>(); if (positions.Length == 0) return null; Stop(serial); var playback = new SnakeImagePlayback(keycard, positions, options); ActivePlaybacks[serial] = playback; playback.Start(); return playback; }
    public static bool Stop(ushort serial) { if (!ActivePlaybacks.TryGetValue(serial, out SnakeImagePlayback playback)) return false; playback.Stop(); return true; }
    public static SnakeImagePlayback PlayFrames(ExiledPlayer player, IReadOnlyList<VideoFrameData> frames, SnakeImageOptions options = null) => PlayFrames((player?.CurrentItem as Keycard)?.Serial ?? 0, frames, options);
    public static SnakeImagePlayback PlayFrames(Keycard keycard, IReadOnlyList<VideoFrameData> frames, SnakeImageOptions options = null) => PlayFrames(keycard?.Serial ?? 0, frames, options);
    public static void StopAll(bool restoreSnake = true) { foreach (SnakeImagePlayback playback in ActivePlaybacks.Values.ToArray()) playback.Stop(restoreSnake, true); ActivePlaybacks.Clear(); }
    internal static void Remove(SnakeImagePlayback playback) { if (ActivePlaybacks.TryGetValue(playback.Serial, out SnakeImagePlayback current) && ReferenceEquals(current, playback)) ActivePlaybacks.Remove(playback.Serial); }
    internal static bool BlocksClientSnakeSync(ushort serial) => ActivePlaybacks.TryGetValue(serial, out SnakeImagePlayback playback) && playback.BlocksClientSnakeSync;
    internal static List<Vector2Int> DefaultSegments() => new() { new Vector2Int(8, 5), new Vector2Int(7, 5), new Vector2Int(6, 5), new Vector2Int(5, 5), new Vector2Int(4, 5) };
    private static List<Vector2Int> CreateSegments(VideoFrameData frame, SnakeImageOptions options)
    { var result = new List<Vector2Int>(); if (options.CustomRenderer != null) return options.CustomRenderer.Render(new SnakeImageFrameContext(frame, options)).ToList(); for (int y = 0; y < options.Height; y++) for (int x = 0; x < options.Width; x++) if (new SnakeImageFrameContext(frame, options).IsForeground(x, y)) result.Add(new Vector2Int(options.OffsetX + x, options.OffsetY + y)); return result.Count == 0 ? DefaultSegments() : result; }
    private static void OnWaitingForPlayers() => StopAll(false);
    private static void OnSnakeMove(SnakeMoveEventArgs ev) { if (ev?.Keycard != null && ActivePlaybacks.TryGetValue(ev.Keycard.Serial, out SnakeImagePlayback playback) && playback.StopsOnSnakeInput) playback.Stop(); }
}
