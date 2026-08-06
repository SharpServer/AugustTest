using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Weapons;
using AntiMeme.Maps;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.CustomHandlers;
using PlayerRoles;
using PlayerRoles.Spectating;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;
using VoiceChat;
using ExiledMap = Exiled.API.Features.Map;
using AntiMeme.Snake;
using AntiMeme.Roles.FoundationForces;
using AntiMeme.Roles.Scps;

namespace AntiMeme.Audio;

/// <summary>Single lifecycle owner for the audio domain.</summary>
public sealed class AudioRuntime : EventHandlerBase
{
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;
    public override void RegisterEvents()
    {
        VoiceRoutingApi.RegisterEvents();
        PlayerSpeakerManager.RegisterEvents();
        ProximityVoice.RegisterEvents();
        SnakeImageApi.RegisterEvents();
        GunSoundResolver.RegisterEvents();
        RegisterLegacyGunSounds();
        MapAudio.Requested += OnMapAudioRequested;
        // AraOrun は SCP チャットに参加できない。役職は型で見る (名前の文字列比較にしない)。
        VoiceRoutingApi.Register(new VoiceRouteRule("audio.ara-orun-scp-chat", context =>
        {
            if (context.SourceChannel != VoiceChatChannel.ScpChat) return null;

            return CustomRole.Of(context.Sender) is AraOrun ? VoiceRouteDecision.Block() : null;
        }, priority: 10000));
    }
    public override void UnregisterEvents()
    {
        VoiceRoutingApi.Unregister("audio.ara-orun-scp-chat");
        MapAudio.Requested -= OnMapAudioRequested;
        GunSoundResolver.UnregisterEvents();
        SnakeImageApi.UnregisterEvents();
        ProximityVoice.UnregisterEvents();
        PlayerSpeakerManager.UnregisterEvents();
        VoiceRoutingApi.UnregisterEvents();
    }
    protected override void OnDisposed() => SpeakerApi.DestroyAll();

    private static void RegisterLegacyGunSounds()
    {
        GunSoundResolver.Register<GunM82>(context =>
        {
            if (context.AudioIndex is not (0 or 1 or 2)) return null;
            ExiledMap.ExplodeEffect(context.Position, ProjectileType.FragGrenade);
            return GunSoundOverride.Silent;
        });

        GunSoundResolver.Register<GunProject90>(context =>
        {
            string clip = GunSoundResolver.GetClipName(context.Firearm, context.AudioIndex);
            if (string.Equals(clip, "CrossvecSilenced", StringComparison.OrdinalIgnoreCase))
                return new GunSoundOverride("GunP90Silence.ogg", 15f);

            return GunSoundResolver.Resolve(context.Firearm, context.AudioIndex, context.Channel) switch
            {
                GunSoundKind.Gunshot => new GunSoundOverride("GunP90Fire.ogg", 15f),
                GunSoundKind.DryFire => new GunSoundOverride("GunP90NoAmmo.ogg", 15f),
                GunSoundKind.Reload => new GunSoundOverride("GunP90Reloading.ogg", 15f),
                _ => null,
            };
        });

        GunSoundResolver.Register<GunSuppressiver>(context =>
            GunSoundResolver.Resolve(context.Firearm, context.AudioIndex, context.Channel) == GunSoundKind.Gunshot
                ? new GunSoundOverride("GunSuppressiver.ogg", 15f)
                : null);
    }

    private static void OnMapAudioRequested(MapAudioRequest request)
    {
        string key = string.IsNullOrWhiteSpace(request.Key) ? request.Clip : request.Key;
        string speaker = $"MapAudio_{key}";

        if (request.Stop)
        {
            SpeakerApi.TryDestroy(speaker);

            return;
        }

        if (request.Loop)
        {
            SpeakerApi.PlayLoop(
                request.Clip,
                speaker,
                request.Position,
                isSpatial: request.MaxDistance < 100f,
                maxDistance: request.MaxDistance,
                minDistance: 1f,
                volume: request.Volume);

            return;
        }

        SpeakerApi.PlayOneShot(
            request.Clip,
            speaker,
            request.Position,
            voices: 4,
            isSpatial: request.MaxDistance < 100f,
            maxDistance: request.MaxDistance,
            minDistance: 1f,
            volume: request.Volume);
    }
}

/// <summary>Proximity mirror for SCP chat. Toggle state is explicit so keybind code can call it later.</summary>
public static class ProximityVoice
{
    private static readonly HashSet<int> Enabled = new();
    private static readonly HashSet<int> Forced = new();
    private static readonly Dictionary<string, RoleAvailability> RoleRules = new(StringComparer.OrdinalIgnoreCase);
    private static bool registered;
    public static float MaxDistance { get; set; } = 20f;
    public static float MinDistance { get; set; } = 7.5f;
    public static float Volume { get; set; } = 1f;
    public static bool IsSpatial { get; set; } = true;
    public static bool IsEnabled(Player player) => player != null && Enabled.Contains(player.Id);
    public static bool CanUse(Player player) => player != null && (Forced.Contains(player.Id) || RoleRules.Values.Any(rule => rule.Available(player)) || player.Role.Type is RoleTypeId.Scp049 or RoleTypeId.Scp939 or RoleTypeId.Scp3114);
    public static void RegisterRole(string id, Predicate<Player> available, bool enabledByDefault = true)
    { if (string.IsNullOrWhiteSpace(id) || available == null) throw new ArgumentException("A proximity role rule is required."); RoleRules[id.Trim()] = new RoleAvailability(available, enabledByDefault); }
    public static bool UnregisterRole(string id) => !string.IsNullOrWhiteSpace(id) && RoleRules.Remove(id.Trim());
    public static void SetForced(Player player, bool forced, bool activate = true)
    { if (player == null) return; if (forced) { Forced.Add(player.Id); if (activate) Enabled.Add(player.Id); } else { Forced.Remove(player.Id); if (!CanUse(player)) Enabled.Remove(player.Id); } }
    public static bool Toggle(Player player)
    { if (!CanUse(player)) return false; if (!Enabled.Add(player.Id)) Enabled.Remove(player.Id); return Enabled.Contains(player.Id); }
    internal static void RegisterEvents()
    { if (registered) return; Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole; Exiled.Events.Handlers.Player.Left += OnLeft; Exiled.Events.Handlers.Server.RestartingRound += OnRoundRestarted; VoiceRoutingApi.Register(new VoiceRouteRule("audio.proximity-scp-chat", Evaluate, 100)); registered = true; }
    internal static void UnregisterEvents()
    { if (!registered) return; Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole; Exiled.Events.Handlers.Player.Left -= OnLeft; Exiled.Events.Handlers.Server.RestartingRound -= OnRoundRestarted; VoiceRoutingApi.Unregister("audio.proximity-scp-chat"); Enabled.Clear(); Forced.Clear(); registered = false; }
    private static VoiceRouteDecision? Evaluate(VoiceRouteContext context)
    { if (context.SourceChannel != VoiceChatChannel.ScpChat || !CanUse(context.Sender) || !Enabled.Contains(context.Sender.Id)) return null; if (context.Receiver == null) return null; if (context.Receiver.ReferenceHub.roleManager.CurrentRole is SpectatorRole) { if (!context.Sender.ReferenceHub.IsSpectatedBy(context.Receiver.ReferenceHub)) return null; } else if (Vector3.Distance(context.Sender.Position, context.Receiver.Position) >= MaxDistance) return null; return VoiceRouteDecision.Spatial(PlayerSpeakerManager.PurposeProximity, MaxDistance, MinDistance, Volume, false); }
    private static void OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (ev.Player == null) return;
        if (!CanUse(ev.Player)) { Enabled.Remove(ev.Player.Id); Forced.Remove(ev.Player.Id); return; }
        if (RoleRules.Values.Any(rule => rule.DefaultEnabled && rule.Available(ev.Player))) Enabled.Add(ev.Player.Id);
    }
    private static void OnLeft(LeftEventArgs ev) { if (ev.Player != null) { Enabled.Remove(ev.Player.Id); Forced.Remove(ev.Player.Id); } }
    private static void OnRoundRestarted() { Enabled.Clear(); Forced.Clear(); }
    private readonly struct RoleAvailability { public RoleAvailability(Predicate<Player> available, bool defaultEnabled) { Available = available; DefaultEnabled = defaultEnabled; } public Predicate<Player> Available { get; } public bool DefaultEnabled { get; } }
}
