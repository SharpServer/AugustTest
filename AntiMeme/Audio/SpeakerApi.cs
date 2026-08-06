using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using UnityEngine;
using VoiceChat;
using VoiceChat.Networking;

using Logger = LabApi.Features.Console.Logger;
using LabSpeakerToy = LabApi.Features.Wrappers.SpeakerToy;
using ExiledPlayer = Exiled.API.Features.Player;
using LabPlayer = LabApi.Features.Wrappers.Player;

namespace AntiMeme.Audio;

/// <summary>Server-owned SpeakerToy playback with explicit cleanup and listener visibility.</summary>
public static class SpeakerApi
{
    private const float MinimumAudibleDistance = 1f;
    private static readonly Dictionary<string, List<Playback>> Playbacks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<LivePlayback>> LivePlaybacks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> Cursors = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<byte> AllocatedIds = new();
    private static readonly Dictionary<byte, CoroutineHandle> Streams = new();

    private readonly struct Clip
    {
        public Clip(string name, float[] samples) { Name = name; Samples = samples; Duration = AudioClips.Duration(samples); }
        public string Name { get; }
        public float[] Samples { get; }
        public float Duration { get; }
    }

    private static readonly Dictionary<string, Clip> Clips = new(StringComparer.OrdinalIgnoreCase);

    public readonly struct Playback
    {
        internal Playback(string player, string clip, LabSpeakerToy speaker, byte id)
        { AudioPlayerName = player; ClipName = clip; Speaker = speaker; ControllerId = id; }
        public string AudioPlayerName { get; }
        public string ClipName { get; }
        public LabSpeakerToy Speaker { get; }
        public byte ControllerId { get; }
        public bool IsValid => Speaker != null && !Speaker.IsDestroyed && ControllerId != 0;
        public float Volume => IsValid ? Speaker.Volume : 0f;
        public bool Stop() => SpeakerApi.Stop(this);
        public bool DestroyAudioPlayer() => Stop();
        public void SetTransform(Vector3 position, Transform parent = null) => SpeakerApi.SetTransform(this, position, parent);
        public void SetVolume(float volume) => SpeakerApi.SetVolume(this, volume);
        public void SetListeners(Predicate<ExiledPlayer> listeners) => SpeakerApi.SetListeners(this, listeners);
    }

    public readonly struct LivePlayback
    {
        internal LivePlayback(string player, LabSpeakerToy speaker, byte id)
        { AudioPlayerName = player; Speaker = speaker; ControllerId = id; }
        public string AudioPlayerName { get; }
        public LabSpeakerToy Speaker { get; }
        public byte ControllerId { get; }
        public bool IsValid => Speaker != null && !Speaker.IsDestroyed && ControllerId != 0;
        public float Volume => IsValid ? Speaker.Volume : 0f;
        public bool DestroyAudioPlayer() => DestroyLiveSpeaker(this);
        public void SetTransform(Vector3 position, Transform parent = null) => SpeakerApi.SetTransform(this, position, parent);
        public void SetVolume(float volume) => SpeakerApi.SetVolume(this, volume);
        public void SetListeners(Predicate<ExiledPlayer> listeners) => SpeakerApi.SetListeners(this, listeners);
        public int SendFrame(byte[] data, int length, IEnumerable<ReferenceHub> targets) => SendAudioFrame(this, data, length, targets);
    }

    public static IEnumerable<string> GetAudioPlayerNames() => Playbacks.Keys.Concat(LivePlaybacks.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public static LivePlayback CreateLiveSpeaker(string audioPlayerName, Vector3 position, Transform parent = null,
        string speakerName = null, bool isSpatial = true, float maxDistance = 5f, float minDistance = 1f,
        float volume = 1f, Predicate<ExiledPlayer> listeners = null)
    {
        if (string.IsNullOrWhiteSpace(audioPlayerName)) throw new ArgumentException("Audio player name cannot be empty.", nameof(audioPlayerName));
        LabSpeakerToy speaker = null;
        byte id = 0;
        try
        {
            speaker = CreateSpeaker(position, parent, isSpatial, maxDistance, minDistance, volume, listeners, out id);
            var playback = new LivePlayback(audioPlayerName, speaker, id);
            Add(LivePlaybacks, audioPlayerName, playback);
            return playback;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Audio] live speaker creation failed for {audioPlayerName}: {ex.Message}");
            if (speaker != null) SafeDestroy(speaker);
            if (id != 0) AllocatedIds.Remove(id);
            return default;
        }
    }

    public static Playback Play(string fileName, string audioPlayerName, Vector3 position, bool destroyOnEnd = false,
        Transform parent = null, bool isSpatial = false, float maxDistance = 5f, float minDistance = 0.1f,
        bool loadClip = true, string speakerName = null, string clipName = null, float volume = 1f,
        Predicate<ExiledPlayer> listeners = null)
        => PlayCore(fileName, audioPlayerName, position, parent, isSpatial, maxDistance, minDistance, loadClip, clipName, volume, listeners, false, destroyOnEnd);

    public static Playback PlayLoop(string fileName, string audioPlayerName, Vector3 position, Transform parent = null,
        bool isSpatial = false, float maxDistance = 5f, float minDistance = 0.1f, bool loadClip = true,
        string speakerName = null, string clipName = null, bool restartIfAlreadyPlaying = true, float volume = 1f,
        Predicate<ExiledPlayer> listeners = null)
    {
        if (restartIfAlreadyPlaying) TryDestroy(audioPlayerName);
        return PlayCore(fileName, audioPlayerName, position, parent, isSpatial, maxDistance, minDistance, loadClip, clipName, volume, listeners, true, false);
    }

    public static Playback PlayOneShot(string fileName, string audioPlayerName, Vector3 position, int voices = 1,
        Transform parent = null, bool isSpatial = true, float maxDistance = 5f, float minDistance = 0.1f,
        string clipName = null, float volume = 1f, Predicate<ExiledPlayer> listeners = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(audioPlayerName)) return default;
        clipName ??= fileName;
        if (!TryLoadClip(fileName, clipName) || !Clips.TryGetValue(clipName, out Clip clip)) return default;
        List<Playback> list = Live(Playbacks, audioPlayerName);
        list.RemoveAll(p => !p.IsValid);
        int wanted = Mathf.Clamp(voices, 1, 8);
        if (list.Count < wanted)
            return PlayCore(fileName, audioPlayerName, position, parent, isSpatial, maxDistance, minDistance, false, clipName, volume, listeners, false, false);
        int cursor = Cursors.TryGetValue(audioPlayerName, out int value) ? value % list.Count : 0;
        Playback playback = list[cursor];
        Cursors[audioPlayerName] = (cursor + 1) % list.Count;
        SetTransform(playback, position, parent);
        try { playback.Speaker.Play(clip.Samples, false, false); return playback; }
        catch { Stop(playback); return default; }
    }

    public static Playback PlaySamples(string audioPlayerName, float[] samples, Vector3 position, Transform parent = null,
        bool isSpatial = true, float maxDistance = 10f, float minDistance = 1f, float volume = 1f, bool loop = false,
        IEnumerable<ExiledPlayer> targets = null, bool destroyOnEnd = true, Predicate<ExiledPlayer> listeners = null)
    {
        if (string.IsNullOrWhiteSpace(audioPlayerName) || samples == null || samples.Length == 0) return default;
        HashSet<string> ids = targets?.Where(p => p != null).Select(p => p.UserId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (ids != null)
        {
            Predicate<ExiledPlayer> old = listeners;
            listeners = p => ids.Contains(p.UserId) && (old == null || old(p));
        }
        LabSpeakerToy speaker = null;
        byte id = 0;
        try
        {
            speaker = CreateSpeaker(position, parent, isSpatial, maxDistance, minDistance, volume, listeners, out id);
            speaker.Play(samples, false, loop);
            var playback = new Playback(audioPlayerName, audioPlayerName, speaker, id);
            Add(Playbacks, audioPlayerName, playback);
            if (destroyOnEnd && !loop) Timing.CallDelayed(AudioClips.Duration(samples) + .75f, () => Stop(playback));
            return playback;
        }
        catch (Exception ex) { Logger.Warn($"[Audio] sample playback failed: {ex.Message}"); if (speaker != null) SafeDestroy(speaker); if (id != 0) AllocatedIds.Remove(id); return default; }
    }

    public static bool TryLoadClip(string fileName, string clipName = null)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        clipName ??= fileName;
        if (Clips.ContainsKey(clipName)) return true;
        float[] samples = AudioClips.Get(fileName);
        if (samples == null || samples.Length == 0) return false;
        Clips[clipName] = new Clip(clipName, samples);
        return true;
    }

    public static void LoadClip(string fileName, string clipName = null)
    { if (!TryLoadClip(fileName, clipName)) throw new InvalidOperationException($"Could not load audio clip '{fileName}'."); }
    public static float GetClipDuration(string clipName) => Clips.TryGetValue(clipName ?? string.Empty, out Clip clip) ? clip.Duration : 0f;

    public static bool Stop(Playback playback)
    {
        if (playback.ControllerId == 0) return false;
        if (Streams.TryGetValue(playback.ControllerId, out CoroutineHandle stream)) Timing.KillCoroutines(stream);
        Streams.Remove(playback.ControllerId);
        bool changed = Remove(Playbacks, playback.AudioPlayerName, playback.ControllerId);
        if (playback.Speaker != null && !playback.Speaker.IsDestroyed) { SafeDestroy(playback.Speaker); changed = true; }
        if (!Tracked(playback.ControllerId)) AllocatedIds.Remove(playback.ControllerId);
        return changed;
    }

    public static bool StopClip(string audioPlayerName, string clipName)
    { return Playbacks.TryGetValue(audioPlayerName ?? string.Empty, out List<Playback> list) && list.Where(p => string.Equals(p.ClipName, clipName, StringComparison.OrdinalIgnoreCase)).ToArray().Any(Stop); }
    public static int StopClip(string clipName) => Playbacks.Values.SelectMany(x => x).Where(p => string.Equals(p.ClipName, clipName, StringComparison.OrdinalIgnoreCase)).ToArray().Count(Stop);
    public static int StopClips(params Playback[] playbacks) => playbacks?.Count(Stop) ?? 0;
    public static bool TryDestroy(string name)
    {
        bool result = false;
        if (Playbacks.TryGetValue(name ?? string.Empty, out List<Playback> p)) foreach (Playback value in p.ToArray()) result |= Stop(value);
        if (LivePlaybacks.TryGetValue(name ?? string.Empty, out List<LivePlayback> l)) foreach (LivePlayback value in l.ToArray()) result |= DestroyLiveSpeaker(value);
        Cursors.Remove(name ?? string.Empty); return result;
    }
    public static int DestroyAll()
    {
        Playback[] p = Playbacks.Values.SelectMany(x => x).ToArray(); LivePlayback[] l = LivePlaybacks.Values.SelectMany(x => x).ToArray();
        foreach (Playback value in p) Stop(value); foreach (LivePlayback value in l) DestroyLiveSpeaker(value);
        Clips.Clear(); Cursors.Clear(); Streams.Clear(); AllocatedIds.Clear(); return p.Length + l.Length;
    }
    public static int PruneInvalid()
    { int count = 0; foreach (Playback p in Playbacks.Values.SelectMany(x => x).Where(x => !x.IsValid).ToArray()) if (Stop(p)) count++; foreach (LivePlayback p in LivePlaybacks.Values.SelectMany(x => x).Where(x => !x.IsValid).ToArray()) if (DestroyLiveSpeaker(p)) count++; return count; }

    public static void SetTransform(Playback p, Vector3 position, Transform parent = null) { if (p.IsValid) SetTransform(p.Speaker, position, parent); }
    public static void SetTransform(LivePlayback p, Vector3 position, Transform parent = null) { if (p.IsValid) SetTransform(p.Speaker, position, parent); }
    public static void SetVolume(Playback p, float volume) { if (p.IsValid) p.Speaker.Volume = Mathf.Max(0f, volume); }
    public static void SetVolume(LivePlayback p, float volume) { if (p.IsValid) p.Speaker.Volume = Mathf.Max(0f, volume); }
    public static void SetListeners(Playback p, Predicate<ExiledPlayer> listeners) { if (p.IsValid) ApplyListeners(p.Speaker, listeners); }
    public static void SetListeners(LivePlayback p, Predicate<ExiledPlayer> listeners) { if (p.IsValid) ApplyListeners(p.Speaker, listeners); }

    public static bool DestroyLiveSpeaker(LivePlayback playback)
    {
        if (playback.ControllerId == 0) return false;
        bool changed = Remove(LivePlaybacks, playback.AudioPlayerName, playback.ControllerId);
        if (playback.Speaker != null && !playback.Speaker.IsDestroyed) { SafeDestroy(playback.Speaker); changed = true; }
        if (!Tracked(playback.ControllerId)) AllocatedIds.Remove(playback.ControllerId); return changed;
    }

    public static int SendAudioFrame(LivePlayback playback, byte[] data, int dataLength, IEnumerable<ReferenceHub> targets)
    {
        if (!playback.IsValid || data == null || dataLength <= 0 || dataLength > data.Length || targets == null) return 0;
        var message = new AudioMessage(playback.ControllerId, data, dataLength); int sent = 0;
        foreach (ReferenceHub target in targets)
        {
            try { if (target?.connectionToClient == null) continue; target.connectionToClient.Send(message); sent++; }
            catch (Exception ex) { Logger.Debug($"[Audio] voice frame delivery failed: {ex.Message}"); }
        }
        return sent;
    }
    public static int SendAudioFrame(string name, byte[] data, int length, IEnumerable<ReferenceHub> targets)
    { return LivePlaybacks.TryGetValue(name ?? string.Empty, out List<LivePlayback> list) ? list.ToArray().Sum(p => SendAudioFrame(p, data, length, targets)) : 0; }

    private static Playback PlayCore(string fileName, string name, Vector3 position, Transform parent, bool spatial, float max, float min,
        bool load, string clipName, float volume, Predicate<ExiledPlayer> listeners, bool loop, bool destroyOnEnd)
    {
        clipName ??= fileName; if ((load || !Clips.ContainsKey(clipName)) && !TryLoadClip(fileName, clipName)) return default;
        if (!Clips.TryGetValue(clipName, out Clip clip)) return default;
        LabSpeakerToy speaker = null;
        byte id = 0;
        try
        {
            speaker = CreateSpeaker(position, parent, spatial, max, min, volume, listeners, out id);
            speaker.Play(clip.Samples, false, loop); var p = new Playback(name, clipName, speaker, id); Add(Playbacks, name, p);
            if (destroyOnEnd && !loop) Timing.CallDelayed(clip.Duration + .75f, () => Stop(p)); return p;
        }
        catch (Exception ex) { Logger.Warn($"[Audio] playback failed for {name}/{clipName}: {ex.Message}"); if (speaker != null) SafeDestroy(speaker); if (id != 0) AllocatedIds.Remove(id); return default; }
    }
    private static LabSpeakerToy CreateSpeaker(Vector3 position, Transform parent, bool spatial, float max, float min, float volume, Predicate<ExiledPlayer> listeners, out byte id)
    {
        id = AllocateId(); min = Mathf.Max(MinimumAudibleDistance, min); max = Mathf.Max(min, max);
        LabSpeakerToy speaker = LabSpeakerToy.Create(parent ? Vector3.zero : position, Quaternion.identity, Vector3.one, parent, false);
        speaker.ControllerId = id; speaker.IsSpatial = spatial; speaker.MaxDistance = max; speaker.MinDistance = min; speaker.Volume = Mathf.Max(0f, volume);
        speaker.Spawn(); ApplyListeners(speaker, listeners); return speaker;
    }
    private static void SetTransform(LabSpeakerToy speaker, Vector3 position, Transform parent)
    { if (parent) { speaker.Transform.SetParent(parent); speaker.Transform.localPosition = Vector3.zero; speaker.Transform.localRotation = Quaternion.identity; } else speaker.Position = position; }
    private static void ApplyListeners(LabSpeakerToy speaker, Predicate<ExiledPlayer> listeners)
    {
        if (listeners == null) { speaker.ValidPlayers = null; return; }
        speaker.ValidPlayers = p => { try { return p != null && listeners(ExiledPlayer.Get(p.ReferenceHub)); } catch { return false; } };
    }
    private static byte AllocateId()
    { PruneInvalid(); for (byte id = 1; id < byte.MaxValue; id++) if (!AllocatedIds.Contains(id) && !LabSpeakerToy.List.Any(s => s.ControllerId == id)) { AllocatedIds.Add(id); return id; } throw new InvalidOperationException("No available SpeakerToy controller IDs."); }
    private static void SafeDestroy(LabSpeakerToy speaker) { try { speaker.Stop(); speaker.Destroy(); } catch (Exception ex) { Logger.Debug($"[Audio] speaker cleanup failed: {ex.Message}"); } }
    private static bool Tracked(byte id) => Playbacks.Values.SelectMany(x => x).Any(p => p.ControllerId == id) || LivePlaybacks.Values.SelectMany(x => x).Any(p => p.ControllerId == id);
    private static void Add<T>(Dictionary<string, List<T>> map, string key, T value) { if (!map.TryGetValue(key, out List<T> list)) map[key] = list = new List<T>(); list.Add(value); }
    private static List<T> Live<T>(Dictionary<string, List<T>> map, string key) { if (!map.TryGetValue(key, out List<T> list)) map[key] = list = new List<T>(); return list; }
    private static bool Remove(Dictionary<string, List<Playback>> map, string key, byte id) { if (!map.TryGetValue(key ?? string.Empty, out List<Playback> list)) return false; bool removed = list.RemoveAll(p => p.ControllerId == id) > 0; if (list.Count == 0) map.Remove(key ?? string.Empty); return removed; }
    private static bool Remove(Dictionary<string, List<LivePlayback>> map, string key, byte id) { if (!map.TryGetValue(key ?? string.Empty, out List<LivePlayback> list)) return false; bool removed = list.RemoveAll(p => p.ControllerId == id) > 0; if (list.Count == 0) map.Remove(key ?? string.Empty); return removed; }
}
