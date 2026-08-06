using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>Tag で一つ以上の <see cref="UsefulDoor"/> を操作するボタンです。</summary>
public sealed class UsefulDoorButton : ObjectPrefab
{
    public string TargetTag { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int MaxActivations { get; set; }
    public int ActivationCount { get; private set; }
    public static event Action<Player, UsefulDoorButton, bool> Pressed;
    protected override string SchematicName => "UsefulDoorButton";

    protected override void OnSetup()
    {
        InteractableHandle handle = GetInteractable("Interactable") ?? AddInteractable(.1f, scale: Vector3.one * .75f);
        handle.Interacting += ev => ev.IsAllowed = Enabled && (MaxActivations <= 0 || ActivationCount < MaxActivations);
        handle.Interacted += ev => Press(ev.Player);
    }

    public bool Press(Player player = null)
    {
        if (!Enabled || (MaxActivations > 0 && ActivationCount >= MaxActivations)) return false;
        bool changed = false;
        foreach (UsefulDoor door in ObjectPrefab.All)
        {
            if (door is UsefulDoor useful && useful.Matches(string.IsNullOrWhiteSpace(TargetTag) ? Tag : TargetTag))
                changed |= useful.TryInteract(player);
        }
        if (changed) ActivationCount++;
        Pressed?.Invoke(player, this, changed);
        return changed;
    }
}
