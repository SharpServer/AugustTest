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

public static class SnakeMediaApi
{
    private static readonly Dictionary<ushort, SnakeMediaPlayback> ActivePlaybacks = new();
    public static IReadOnlyCollection<SnakeMediaPlayback> Active => ActivePlaybacks.Values.ToArray();
    public static SnakeMediaPlayback PlayPixelMedia(ExiledPlayer player, string location, SnakeMediaOptions options = null) { Keycard card = player?.CurrentItem as Keycard; return PlayPixelMedia(card, location, options); }
    public static SnakeMediaPlayback PlayPixelMedia(Keycard card, string location, SnakeMediaOptions options = null)
    {
        if (card == null) return null;
        options ??= new SnakeMediaOptions();
        var playback = new SnakeMediaPlayback(card.Serial) { IsLoading = true };
        ActivePlaybacks[card.Serial] = playback;
        string source = location;
        bool downloaded = false;
        try
        {
            if (options.Image.UseYtDlp && YtDlp.IsUrl(source)) { source = YtDlp.Download(source); downloaded = source != null; }
            if (source == null) throw new InvalidOperationException("Media download failed.");
            SnakeImagePlayback image = SnakeImageApi.PlayFile(card.Serial, source, options.Image);
            float[] samples = Ffmpeg.DecodeAudio(source);
            SpeakerApi.Playback audio = default;
            if (samples != null && samples.Length > 0 && card.Owner != null)
                audio = SpeakerApi.PlaySamples(options.AudioPlayerName ?? $"SnakeMedia_{card.Serial}", samples, card.Owner.Position, isSpatial: options.IsSpatial, maxDistance: options.MaxDistance, minDistance: options.MinDistance, volume: options.Volume, loop: options.Image.Loop, destroyOnEnd: !options.Image.Loop, listeners: options.Listeners);
            playback.Attach(image, audio);
            return playback;
        }
        catch (Exception ex) { playback.Error = ex; playback.Stop(); return playback; }
        finally { if (downloaded) YtDlp.Discard(source); }
    }
    public static SnakeMediaPlayback Play(ExiledPlayer player, string location, SnakeMediaOptions options = null) => PlayPixelMedia(player, location, options);
    public static SnakeMediaPlayback Play(Keycard card, string location, SnakeMediaOptions options = null) => PlayPixelMedia(card, location, options);
    public static SnakeMediaPlayback Play(ushort serial, string location, SnakeMediaOptions options = null) => PlayPixelMedia(Item.Get(serial) as Keycard, location, options);
    public static SnakeMediaPlayback PlayPixelMedia(ushort serial, string location, SnakeMediaOptions options = null) => PlayPixelMedia(Item.Get(serial) as Keycard, location, options);
    public static bool Stop(ushort serial) { if (!ActivePlaybacks.TryGetValue(serial, out SnakeMediaPlayback playback)) return false; playback.Stop(); return true; }
    public static void StopAll(bool restoreSnake = true) { foreach (SnakeMediaPlayback playback in ActivePlaybacks.Values.ToArray()) playback.Stop(); ActivePlaybacks.Clear(); }
    internal static void Remove(SnakeMediaPlayback playback) { if (ActivePlaybacks.TryGetValue(playback.Serial, out SnakeMediaPlayback current) && ReferenceEquals(current, playback)) ActivePlaybacks.Remove(playback.Serial); }
}
