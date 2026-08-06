using System;
using System.Collections.Generic;
using System.Linq;
using AudioPooling;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using Sliced.API.Features;
using UnityEngine;
using ExiledFirearm = Exiled.API.Features.Items.Firearm;
using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Audio;

public enum GunSoundKind { Gunshot, DryFire, Reload, Unload, Equip, RevolverCocking, RevolverDecocking, RevolverDoubleAction, DisruptorAction }

public readonly struct GunSoundSelector : IEquatable<GunSoundSelector>
{
    private readonly string clip;
    private readonly int index;
    private readonly GunSoundKind kind;
    private readonly MixerChannel channel;
    private readonly byte type;
    private GunSoundSelector(byte type, GunSoundKind kind, string clip, MixerChannel channel, int index) { this.type = type; this.kind = kind; this.clip = clip; this.channel = channel; this.index = index; }
    public static GunSoundSelector FromKind(GunSoundKind kind) => new(1, kind, null, default, 0);
    public static GunSoundSelector FromClip(string clip) => new(2, default, clip ?? string.Empty, default, 0);
    public static GunSoundSelector FromChannel(MixerChannel channel) => new(3, default, null, channel, 0);
    public static GunSoundSelector FromIndex(int index) => new(4, default, null, default, index);
    public static implicit operator GunSoundSelector(GunSoundKind value) => FromKind(value);
    public static implicit operator GunSoundSelector(string value) => FromClip(value);
    public bool Equals(GunSoundSelector other) => type == other.type && kind == other.kind && index == other.index && channel == other.channel && string.Equals(clip, other.clip, StringComparison.OrdinalIgnoreCase);
    public override bool Equals(object obj) => obj is GunSoundSelector other && Equals(other);
    public override int GetHashCode() => (type * 397) ^ (int)kind ^ index ^ (int)channel ^ StringComparer.OrdinalIgnoreCase.GetHashCode(clip ?? string.Empty);
}

public sealed class GunSoundOverride
{
    public GunSoundOverride(string audioFile, float range = 30f, float volume = 1f) { AudioFile = audioFile; Range = range; Volume = volume; }
    public string AudioFile { get; }
    public float Range { get; }
    public float Volume { get; }
    public bool IsSilent => string.IsNullOrWhiteSpace(AudioFile);
    public static GunSoundOverride Silent => new(null);
    public static implicit operator GunSoundOverride(string audio) => new(audio);
}

public readonly struct GunSoundContext
{
    public GunSoundContext(Player player, ExiledFirearm firearm, int audioIndex, MixerChannel channel, float range, float pitch, Vector3 position)
    { Player = player; Firearm = firearm; AudioIndex = audioIndex; Channel = channel; Range = range; Pitch = pitch; Position = position; }
    public Player Player { get; }
    public ExiledFirearm Firearm { get; }
    public int AudioIndex { get; }
    public MixerChannel Channel { get; }
    public float Range { get; }
    public float Pitch { get; }
    public Vector3 Position { get; }
}

/// <summary>Resolves game firearm audio and offers type-keyed custom item sound overrides.</summary>
public static class GunSoundResolver
{
    private static readonly Dictionary<Type, Func<GunSoundContext, GunSoundOverride>> Providers = new();
    private static readonly HashSet<ushort> CaptureSerials = new();
    private static bool registered;
    public readonly struct ClipEntry
    { public ClipEntry(int index, string name, GunSoundKind? kind) { Index = index; Name = name; Kind = kind; } public int Index { get; } public string Name { get; } public GunSoundKind? Kind { get; } public override string ToString() => $"[{Index}] {Name}" + (Kind.HasValue ? $" ({Kind})" : string.Empty); }
    public static void Register<T>(Func<GunSoundContext, GunSoundOverride> resolver) where T : CustomItem => Register(typeof(T), resolver);
    public static void Register(Type itemType, Func<GunSoundContext, GunSoundOverride> resolver) { if (itemType == null || resolver == null) throw new ArgumentNullException(); Providers[itemType] = resolver; }
    public static bool Unregister(Type itemType) => itemType != null && Providers.Remove(itemType);
    public static void SetCapture(ushort serial, bool enabled) { if (serial == 0) return; if (enabled) CaptureSerials.Add(serial); else CaptureSerials.Remove(serial); }
    internal static void RegisterEvents()
    { if (registered) return; Exiled.Events.Handlers.Player.SendingGunSound += OnSending; Exiled.Events.Handlers.Player.ReceivingGunSound += OnReceiving; registered = true; }
    internal static void UnregisterEvents()
    { if (!registered) return; Exiled.Events.Handlers.Player.SendingGunSound -= OnSending; Exiled.Events.Handlers.Player.ReceivingGunSound -= OnReceiving; Providers.Clear(); registered = false; }

    public static AudioClip GetClip(ExiledFirearm firearm, int audioIndex)
    { List<AudioClip> clips = firearm?.Base?.Modules?.OfType<AudioModule>().FirstOrDefault()?._registeredClips; return clips != null && audioIndex >= 0 && audioIndex < clips.Count ? clips[audioIndex] : null; }
    public static string GetClipName(ExiledFirearm firearm, int audioIndex) => GetClip(firearm, audioIndex)?.name;
    public static GunSoundKind? Resolve(ExiledFirearm firearm, int audioIndex, MixerChannel channel)
    { if (channel == MixerChannel.Weapons) return GunSoundKind.Gunshot; AudioClip clip = GetClip(firearm, audioIndex); if (clip == null || firearm?.Base?.Modules == null) return null; foreach (ModuleBase module in firearm.Base.Modules) { if (module is AutomaticActionModule a && a._dryfireSound == clip) return GunSoundKind.DryFire; if (module is DoubleActionModule d && d._dryFireClip == clip) return GunSoundKind.DryFire; if (module is PumpActionModule p && p._dryFireClip == clip) return GunSoundKind.DryFire; } foreach (ModuleBase module in firearm.Base.Modules) if (module is IReloaderModule r) { if (r.IsReloading) return GunSoundKind.Reload; if (r.IsUnloading) return GunSoundKind.Unload; } return null; }
    public static IReadOnlyList<ClipEntry> DumpClips(ExiledFirearm firearm)
    { List<AudioClip> clips = firearm?.Base?.Modules?.OfType<AudioModule>().FirstOrDefault()?._registeredClips; if (clips == null) return Array.Empty<ClipEntry>(); var result = new List<ClipEntry>(clips.Count); for (int i = 0; i < clips.Count; i++) result.Add(new ClipEntry(i, clips[i]?.name ?? "<null>", clips[i] == null ? null : Resolve(firearm, i, default))); return result; }

    private static void OnSending(SendingGunSoundEventArgs ev)
    {
        if (!ev.IsAllowed || ev.Firearm == null) return;
        if (CaptureSerials.Contains(ev.Firearm.Serial))
            Logger.Info($"[GunSoundTestbench] serial={ev.Firearm.Serial} audioIndex={ev.AudioIndex} clip='{GetClipName(ev.Firearm, ev.AudioIndex) ?? "<unknown>"}' kind={Resolve(ev.Firearm, ev.AudioIndex, ev.MixerChannel)?.ToString() ?? "<unknown>"} channel={ev.MixerChannel}");
        CustomItem item = CustomItem.Of(ev.Firearm.Serial);
        Func<GunSoundContext, GunSoundOverride> resolver = Find(item?.GetType());
        if (resolver == null) return;
        GunSoundOverride sound; try { sound = resolver(new GunSoundContext(ev.Player, ev.Firearm, ev.AudioIndex, ev.MixerChannel, ev.Range, ev.Pitch, ev.SendingPosition)); } catch { return; }
        if (sound == null) return;
        ev.IsAllowed = false;
        if (sound.IsSilent) return;
        SpeakerApi.PlayOneShot(sound.AudioFile, $"GunSound_{ev.Firearm.Serial}", ev.SendingPosition, voices: 8, isSpatial: true, maxDistance: sound.Range, minDistance: 1f, volume: sound.Volume, listeners: p => p != null && Vector3.Distance(p.Position, ev.SendingPosition) <= sound.Range);
    }
    private static void OnReceiving(ReceivingGunSoundEventArgs ev) { }
    private static Func<GunSoundContext, GunSoundOverride> Find(Type type) => type == null ? null : Providers.Where(x => x.Key.IsAssignableFrom(type)).Select(x => x.Value).FirstOrDefault();
}
