using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>二状態のレバーです。外部機能は Toggling/Toggled 通知で接続できます。</summary>
public sealed class InteractableLever : ObjectPrefab
{
    public bool IsOn { get; private set; }
    public bool CanInteract { get; set; } = true;
    /// <summary>切り替えを止められる通知です。状況で塞ぎたい機能 (停電など) が使います。</summary>
    public delegate void TogglingHandler(Player player, InteractableLever lever, bool next, ref bool allowed);

    public static event TogglingHandler Toggling;
    public static event Action<Player, InteractableLever, bool> Toggled;
    protected override string SchematicName => "InteractableLever";

    protected override void OnSetup()
    {
        InteractableHandle handle = GetInteractable("Interactable") ?? AddInteractable(.05f, scale: Vector3.one * .25f);
        handle.Interacting += ev => ev.IsAllowed = CanInteract;
        handle.Interacted += ev => Toggle(ev.Player);
        PlayAnimation();
    }

    public bool Toggle(Player player = null)
    {
        if (!CanInteract) return false;
        bool next = !IsOn;
        bool allowed = true;
        Toggling?.Invoke(player, this, next, ref allowed);
        if (!allowed) return false;
        IsOn = next;
        PlayAnimation();
        MapAudio.Play("LeverFlip.ogg", "Lever", Position);
        Toggled?.Invoke(player, this, IsOn);
        return true;
    }

    private void PlayAnimation()
    {
        try { Schematic?.AnimationController.Play(IsOn ? "Mode1" : "Mode0"); } catch { }
    }
}
