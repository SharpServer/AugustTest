using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerRoles.Voice;
using UnityEngine;
using VoiceChat;
using VoiceChat.Networking;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Audio;

public sealed class VoiceRouteContext
{
    internal VoiceRouteContext(Player sender, Player receiver, VoiceMessage message, VoiceChatChannel source, VoiceChatChannel native)
    { Sender = sender; Receiver = receiver; Message = message; SourceChannel = source; NativeChannel = native; }
    public Player Sender { get; }
    public Player Receiver { get; }
    public VoiceMessage Message { get; }
    public VoiceChatChannel SourceChannel { get; }
    public VoiceChatChannel NativeChannel { get; }
}

public readonly struct VoiceRouteDecision
{
    private VoiceRouteDecision(bool suppress, string key, VoiceChatChannel direct, bool spatial, float max, float min, float volume)
    { SuppressNative = suppress; DeliveryKey = key; DirectChannel = direct; IsSpatial = spatial; MaxDistance = max; MinDistance = min; Volume = volume; }
    public bool SuppressNative { get; }
    public string DeliveryKey { get; }
    public VoiceChatChannel DirectChannel { get; }
    public bool HasDirectDelivery => DirectChannel != VoiceChatChannel.None;
    public bool HasSpatialDelivery => !string.IsNullOrWhiteSpace(DeliveryKey);
    public bool HasDelivery => HasDirectDelivery || HasSpatialDelivery;
    public bool IsSpatial { get; }
    public float MaxDistance { get; }
    public float MinDistance { get; }
    public float Volume { get; }
    public static VoiceRouteDecision Block() => new(true, null, VoiceChatChannel.None, false, 1f, 1f, 1f);
    public static VoiceRouteDecision Direct(bool suppressNative = true) => Direct(VoiceChatChannel.RoundSummary, suppressNative);
    public static VoiceRouteDecision Direct(VoiceChatChannel channel, bool suppressNative = true)
    { if (channel == VoiceChatChannel.None) throw new ArgumentException("A direct voice channel is required.", nameof(channel)); return new(suppressNative, null, channel, false, 1f, 1f, 1f); }
    public static VoiceRouteDecision Spatial(string key, float maxDistance, float minDistance, float volume = 1f, bool suppressNative = false)
    { if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A voice delivery key is required.", nameof(key)); float max = Mathf.Max(1f, maxDistance); return new(suppressNative, key.Trim(), VoiceChatChannel.None, true, max, Mathf.Clamp(minDistance, 1f, max), Mathf.Max(0f, volume)); }
}

public sealed class VoiceRouteRule
{
    public VoiceRouteRule(string id, Func<VoiceRouteContext, VoiceRouteDecision?> evaluator, int priority = 0)
    { if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A voice route id is required.", nameof(id)); Id = id.Trim(); Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator)); Priority = priority; }
    public string Id { get; }
    public int Priority { get; }
    public Func<VoiceRouteContext, VoiceRouteDecision?> Evaluator { get; }
    public static VoiceRouteRule ForPlayers(string id, Predicate<Player> senders, Predicate<Player> receivers, VoiceRouteDecision decision, Predicate<VoiceRouteContext> condition = null, int priority = 0)
    { if (senders == null || receivers == null) throw new ArgumentNullException(); return new(id, c => senders(c.Sender) && receivers(c.Receiver) && (condition == null || condition(c)) ? decision : null, priority); }
}

/// <summary>Pair-wise voice routing. Native voice remains untouched unless a rule explicitly suppresses it.</summary>
public static class VoiceRoutingApi
{
    private static readonly Dictionary<string, Registered> Rules = new(StringComparer.OrdinalIgnoreCase);
    private static long sequence;
    private static bool registered;
    public static IReadOnlyCollection<VoiceRouteRule> RegisteredRules => Rules.Values.OrderByDescending(r => r.Rule.Priority).ThenByDescending(r => r.Sequence).Select(r => r.Rule).ToArray();
    public static void Register(VoiceRouteRule rule) { if (rule == null) throw new ArgumentNullException(nameof(rule)); Rules[rule.Id] = new Registered(rule, ++sequence); }
    public static bool Unregister(string id) => !string.IsNullOrWhiteSpace(id) && Rules.Remove(id.Trim());
    public static void ClearRules() { Rules.Clear(); sequence = 0; }
    internal static void RegisterEvents()
    { if (registered) return; Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting; Exiled.Events.Handlers.Player.ReceivingVoiceMessage += OnReceivingVoiceMessage; registered = true; }
    internal static void UnregisterEvents()
    { if (!registered) return; Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting; Exiled.Events.Handlers.Player.ReceivingVoiceMessage -= OnReceivingVoiceMessage; ClearRules(); registered = false; }

    private static void OnVoiceChatting(VoiceChattingEventArgs ev)
    {
        if (!ev.IsAllowed || !Usable(ev.Player) || ev.VoiceMessage.Data == null || ev.VoiceMessage.DataLength <= 0) return;
        var batches = new Dictionary<string, List<ReferenceHub>>(StringComparer.OrdinalIgnoreCase);
        VoiceChatChannel source = ev.VoiceModule.CurrentChannel;
        foreach (ReferenceHub hub in ReferenceHub.AllHubs)
        {
            if (hub?.connectionToClient == null) continue;
            Player receiver = Player.Get(hub); if (!Usable(receiver)) continue;
            VoiceChatChannel native = hub.roleManager.CurrentRole is IVoiceRole role ? role.VoiceModule.ValidateReceive(ev.Player.ReferenceHub, source) : VoiceChatChannel.None;
            VoiceRouteDecision? decision = Resolve(new VoiceRouteContext(ev.Player, receiver, ev.VoiceMessage, source, native));
            if (decision == null || !decision.Value.HasDelivery) continue;
            VoiceRouteDecision route = decision.Value;
            if (route.HasDirectDelivery) { VoiceMessage direct = ev.VoiceMessage; direct.Channel = route.DirectChannel; hub.connectionToClient.Send(direct); }
            if (route.HasSpatialDelivery) { if (!batches.TryGetValue(route.DeliveryKey, out List<ReferenceHub> list)) batches[route.DeliveryKey] = list = new List<ReferenceHub>(); list.Add(hub); }
        }
        foreach (var pair in batches)
        {
            VoiceRouteDecision? decision = Resolve(new VoiceRouteContext(ev.Player, Player.Get(pair.Value[0]), ev.VoiceMessage, source, VoiceChatChannel.None));
            if (decision == null) continue;
            SpeakerApi.LivePlayback speaker = PlayerSpeakerManager.GetOrCreateSpeaker(ev.Player, decision.Value.DeliveryKey, decision.Value.IsSpatial, decision.Value.MaxDistance, decision.Value.MinDistance, decision.Value.Volume, decision.Value.DeliveryKey);
            if (speaker.IsValid) speaker.SendFrame(ev.VoiceMessage.Data, ev.VoiceMessage.DataLength, pair.Value);
        }
    }

    private static void OnReceivingVoiceMessage(ReceivingVoiceMessageEventArgs ev)
    {
        if (!ev.IsAllowed || !Usable(ev.Sender) || !Usable(ev.Player)) return;
        VoiceRouteDecision? decision = Resolve(new VoiceRouteContext(ev.Sender, ev.Player, ev.VoiceMessage, ev.VoiceModule.CurrentChannel, ev.VoiceMessage.Channel));
        if (decision?.SuppressNative == true) ev.IsAllowed = false;
    }
    private static VoiceRouteDecision? Resolve(VoiceRouteContext context)
    { foreach (Registered entry in Rules.Values.OrderByDescending(r => r.Rule.Priority).ThenByDescending(r => r.Sequence)) try { VoiceRouteDecision? result = entry.Rule.Evaluator(context); if (result != null) return result; } catch (Exception ex) { Logger.Warn($"[Audio] voice rule '{entry.Rule.Id}' failed: {ex.Message}"); } return null; }
    private static bool Usable(Player player) { try { return player != null && player.ReferenceHub != null && !player.IsNPC; } catch { return false; } }
    private readonly struct Registered { public Registered(VoiceRouteRule rule, long sequence) { Rule = rule; Sequence = sequence; } public VoiceRouteRule Rule { get; } public long Sequence { get; } }
}
