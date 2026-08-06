using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.Handlers;
using MEC;
using UnityEngine;
using ExiledPlayer = Exiled.API.Features.Player;

namespace AntiMeme.Audio;

/// <summary>Owns player-following speakers and destroys them on role/death/leave/round transitions.</summary>
public static class PlayerSpeakerManager
{
    public const string PurposeProximity = "proximity";
    public const string PurposeInternalMusic = "internal_music";
    public const string PurposeChaseTheme = "chase_theme";
    private static readonly Dictionary<int, Dictionary<string, SpeakerApi.LivePlayback>> Speakers = new();
    private static readonly Dictionary<int, Dictionary<string, SpeakerApi.Playback>> Playbacks = new();
    private static readonly Dictionary<int, Dictionary<string, CoroutineHandle>> Follows = new();
    private static bool registered;

    internal static void RegisterEvents()
    {
        if (registered) return;
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Server.RestartingRound += OnRoundRestarted;
        registered = true;
    }
    internal static void UnregisterEvents()
    {
        if (!registered) return;
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
        Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
        Exiled.Events.Handlers.Player.Died -= OnDied;
        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Server.RestartingRound -= OnRoundRestarted;
        DestroyAll(); registered = false;
    }

    public static bool TryGetSpeaker(ExiledPlayer player, string purpose, out SpeakerApi.LivePlayback speaker)
    {
        speaker = default;
        return player != null && !string.IsNullOrWhiteSpace(purpose) && Speakers.TryGetValue(player.Id, out Dictionary<string, SpeakerApi.LivePlayback> values) && values.TryGetValue(purpose, out speaker) && speaker.IsValid;
    }

    public static SpeakerApi.LivePlayback GetOrCreateSpeaker(ExiledPlayer player, string purpose, bool isSpatial, float maxDistance, float minDistance,
        float volume = 1f, string speakerName = null, Predicate<ExiledPlayer> listeners = null)
    {
        if (player == null || string.IsNullOrWhiteSpace(purpose)) return default;
        if (!Speakers.TryGetValue(player.Id, out Dictionary<string, SpeakerApi.LivePlayback> dict)) Speakers[player.Id] = dict = new Dictionary<string, SpeakerApi.LivePlayback>(StringComparer.OrdinalIgnoreCase);
        if (dict.TryGetValue(purpose, out SpeakerApi.LivePlayback existing) && existing.IsValid) { existing.SetListeners(listeners); return existing; }
        if (dict.ContainsKey(purpose)) SpeakerApi.DestroyLiveSpeaker(dict[purpose]);
        SpeakerApi.LivePlayback speaker = SpeakerApi.CreateLiveSpeaker($"PlayerSpeaker_{player.Id}_{purpose}", player.Position, speakerName: speakerName ?? purpose, isSpatial: isSpatial, maxDistance: maxDistance, minDistance: minDistance, volume: volume, listeners: listeners);
        if (!speaker.IsValid) return default;
        dict[purpose] = speaker;
        if (!Follows.TryGetValue(player.Id, out Dictionary<string, CoroutineHandle> follows)) Follows[player.Id] = follows = new Dictionary<string, CoroutineHandle>(StringComparer.OrdinalIgnoreCase);
        if (follows.TryGetValue(purpose, out CoroutineHandle old)) Timing.KillCoroutines(old);
        follows[purpose] = Timing.RunCoroutine(Follow(player.Id, purpose, speaker));
        return speaker;
    }

    public static SpeakerApi.Playback Play(ExiledPlayer player, string fileName, string purpose = null, bool isSpatial = true, float maxDistance = 5f, float minDistance = 1f, float volume = 1f, Predicate<ExiledPlayer> listeners = null)
        => PlayManaged(player, fileName, purpose, false, isSpatial, maxDistance, minDistance, volume, listeners);
    public static SpeakerApi.Playback PlayLoop(ExiledPlayer player, string fileName, string purpose = null, bool isSpatial = true, float maxDistance = 5f, float minDistance = 1f, float volume = 1f, Predicate<ExiledPlayer> listeners = null)
        => PlayManaged(player, fileName, purpose, true, isSpatial, maxDistance, minDistance, volume, listeners);
    public static bool TryGetPlayback(ExiledPlayer player, string purpose, out SpeakerApi.Playback playback) { playback = default; return player != null && Playbacks.TryGetValue(player.Id, out Dictionary<string, SpeakerApi.Playback> values) && values.TryGetValue(purpose ?? string.Empty, out playback) && playback.IsValid; }
    public static bool Stop(ExiledPlayer player, string purpose) => player != null && Stop(player.Id, purpose);
    public static bool Stop(int playerId, string purpose)
    { if (!Playbacks.TryGetValue(playerId, out Dictionary<string, SpeakerApi.Playback> values) || !values.TryGetValue(purpose ?? string.Empty, out SpeakerApi.Playback playback)) return false; bool result = SpeakerApi.Stop(playback); values.Remove(purpose ?? string.Empty); if (values.Count == 0) Playbacks.Remove(playerId); return result; }
    public static void DestroySpeaker(ExiledPlayer player, string purpose) { if (player != null) DestroySpeaker(player.Id, purpose); }
    public static void DestroySpeaker(int playerId, string purpose)
    { if (Speakers.TryGetValue(playerId, out Dictionary<string, SpeakerApi.LivePlayback> values) && values.TryGetValue(purpose ?? string.Empty, out SpeakerApi.LivePlayback speaker)) SpeakerApi.DestroyLiveSpeaker(speaker); if (Follows.TryGetValue(playerId, out Dictionary<string, CoroutineHandle> follows) && follows.TryGetValue(purpose ?? string.Empty, out CoroutineHandle handle)) { Timing.KillCoroutines(handle); follows.Remove(purpose ?? string.Empty); } if (Speakers.TryGetValue(playerId, out values)) { values.Remove(purpose ?? string.Empty); if (values.Count == 0) Speakers.Remove(playerId); } }
    public static void DestroyAllForPlayer(ExiledPlayer player) { if (player != null) DestroyAllForPlayer(player.Id); }
    public static void DestroyAllForPlayer(int id) { if (Follows.TryGetValue(id, out Dictionary<string, CoroutineHandle> follows)) foreach (CoroutineHandle h in follows.Values) Timing.KillCoroutines(h); Follows.Remove(id); if (Speakers.TryGetValue(id, out Dictionary<string, SpeakerApi.LivePlayback> speakers)) foreach (SpeakerApi.LivePlayback s in speakers.Values) SpeakerApi.DestroyLiveSpeaker(s); Speakers.Remove(id); if (Playbacks.TryGetValue(id, out Dictionary<string, SpeakerApi.Playback> playbacks)) foreach (SpeakerApi.Playback p in playbacks.Values) SpeakerApi.Stop(p); Playbacks.Remove(id); }
    public static void DestroyAll() { foreach (int id in new List<int>(Speakers.Keys)) DestroyAllForPlayer(id); foreach (int id in new List<int>(Playbacks.Keys)) DestroyAllForPlayer(id); }

    private static SpeakerApi.Playback PlayManaged(ExiledPlayer player, string file, string purpose, bool loop, bool spatial, float max, float min, float volume, Predicate<ExiledPlayer> listeners)
    {
        if (player == null || string.IsNullOrWhiteSpace(file)) return default;
        purpose ??= file;
        Stop(player, purpose);
        SpeakerApi.Playback playback = loop ? SpeakerApi.PlayLoop(file, $"PlayerPlayback_{player.Id}_{purpose}", player.Position, isSpatial: spatial, maxDistance: max, minDistance: min, volume: volume, listeners: listeners) : SpeakerApi.Play(file, $"PlayerPlayback_{player.Id}_{purpose}", player.Position, destroyOnEnd: true, isSpatial: spatial, maxDistance: max, minDistance: min, volume: volume, listeners: listeners);
        if (!playback.IsValid) return default;
        if (!Playbacks.TryGetValue(player.Id, out Dictionary<string, SpeakerApi.Playback> values)) Playbacks[player.Id] = values = new Dictionary<string, SpeakerApi.Playback>(StringComparer.OrdinalIgnoreCase);
        values[purpose] = playback; return playback;
    }
    private static IEnumerator<float> Follow(int id, string purpose, SpeakerApi.LivePlayback speaker)
    { while (speaker.IsValid && Speakers.TryGetValue(id, out Dictionary<string, SpeakerApi.LivePlayback> values) && values.TryGetValue(purpose, out SpeakerApi.LivePlayback current) && current.ControllerId == speaker.ControllerId) { ExiledPlayer player = ExiledPlayer.Get(id); if (player == null || player.ReferenceHub == null) break; speaker.SetTransform(player.Position); yield return Timing.WaitForOneFrame; } DestroySpeaker(id, purpose); }
    private static void OnSpawned(SpawnedEventArgs ev) => DestroyAllForPlayer(ev.Player);
    private static void OnChangingRole(ChangingRoleEventArgs ev) => DestroyAllForPlayer(ev.Player);
    private static void OnDied(DiedEventArgs ev) => DestroyAllForPlayer(ev.Player);
    private static void OnLeft(LeftEventArgs ev) => DestroyAllForPlayer(ev.Player);
    private static void OnRoundRestarted() => DestroyAll();
}
