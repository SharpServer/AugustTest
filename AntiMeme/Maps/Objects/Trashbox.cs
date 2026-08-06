using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>一人三回まで調べられる、ラウンド状態を持つゴミ箱です。</summary>
public sealed class Trashbox : ObjectPrefab
{
    private readonly Dictionary<int, List<TrashboxEventType>> eventsByPlayer =
        new Dictionary<int, List<TrashboxEventType>>();
    private static readonly Dictionary<int, byte> SecretCounts = new Dictionary<int, byte>();

    public enum TrashboxEventType
    {
        Nothing,
        Painkillers,
        DataCell,
        AccessTuner,
        Secret,
    }

    public static event Action<Player, TrashboxEventType> EventTriggered;
    public static bool HimselfTriggered { get; private set; }
    public int TriggeredEventCount
    {
        get
        {
            int count = 0;
            foreach (List<TrashboxEventType> list in eventsByPlayer.Values) count += list.Count;
            return count;
        }
    }

    protected override string SchematicName => "trashbox_obj";

    protected override void OnCreate() => eventsByPlayer.Clear();

    protected override void OnSetup()
    {
        InteractableHandle handle = GetInteractable("Interactable") ?? AddInteractable(1f, scale: Vector3.one * 0.75f);
        handle.Interacted += HandleInteracted;
    }

    protected override void OnDestroy() => eventsByPlayer.Clear();

    private void HandleInteracted(PlayerSearchedToyEventArgs ev)
    {
        if (ev?.Player is not { IsDestroyed: false } player) return;

        if (!eventsByPlayer.TryGetValue(player.PlayerId, out List<TrashboxEventType> triggered))
            eventsByPlayer[player.PlayerId] = triggered = new List<TrashboxEventType>();

        if (triggered.Count >= 3)
        {
            player.SendHint("<color=yellow>もうここには何もないようだ</color>", 5f);
            return;
        }

        TrashboxEventType result = (TrashboxEventType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(TrashboxEventType)).Length);
        if (HimselfTriggered && result == TrashboxEventType.AccessTuner) result = TrashboxEventType.Secret;
        triggered.Add(result);

        switch (result)
        {
            case TrashboxEventType.Painkillers:
                player.AddItem(ItemType.Painkillers);
                player.SendHint("<color=yellow>鎮痛剤を手に入れた！</color>", 5f);
                break;
            case TrashboxEventType.DataCell:
                CustomItem.Give(typeof(Items.Keycards.DataCellLv1), player);
                player.SendHint("<color=yellow>データセルを手に入れた！</color>", 5f);
                break;
            case TrashboxEventType.AccessTuner:
                CustomItem.Give(typeof(Items.Keycards.AccessTunerLv1), player);
                player.SendHint("<color=yellow>Access Tuner を手に入れた！</color>", 5f);
                HimselfTriggered = true;
                break;
            case TrashboxEventType.Secret:
                byte count = SecretCounts.TryGetValue(player.PlayerId, out byte current) ? current : (byte)0;
                SecretCounts[player.PlayerId] = (byte)((count + 1) % 5);
                player.SendHint("<color=yellow>不思議な曲が流れてきた。</color>", 7f);
                MapAudio.Play("5egg_0.ogg", "Trashbox", Schematic?.Position ?? Position, maxDistance: 12f);
                break;
            default:
                player.SendHint("しかし、何も見つからなかった。", 5f);
                break;
        }

        EventTriggered?.Invoke(player, result);
    }

    public static void ResetRoundState()
    {
        SecretCounts.Clear();
        HimselfTriggered = false;
    }
}
