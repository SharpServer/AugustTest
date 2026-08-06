using System;
using System.Linq;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>ボタンや直接操作から同一状態へ収束するマップ扉です。</summary>
public sealed class UsefulDoor : ObjectPrefab
{
    public const string CentralSchematicName = "UsefulDoor";
    public bool Enabled { get; set; } = true;
    public bool Locked { get; set; }
    public DoorPermissionFlags KeycardPermissions { get; set; }
    public bool CanClose { get; set; }
    public bool OneWay { get => !CanClose; set => CanClose = !value; }
    public bool IsOpen { get; private set; }
    public float TransitionDuration { get; set; } = 1f;
    public static event Func<Player, UsefulDoor, bool> Authorize;

    /// <summary>操作を止められる通知です。権限ではなく状況で塞ぎたい機能 (停電など) が使います。</summary>
    public delegate void InteractingHandler(Player player, UsefulDoor door, ref bool allowed);

    public static event InteractingHandler Interacting;

    protected override string SchematicName => CentralSchematicName;

    protected override void OnSetup()
    {
        InteractableHandle handle = GetInteractable("Interactable") ?? AddInteractable(0.1f, scale: Vector3.one * .75f);
        handle.Interacting += ev => ev.IsAllowed = Enabled && !Locked;
        handle.Interacted += ev => TryInteract(ev.Player);
        PlayState();
    }

    public bool TryInteract(Player player = null)
    {
        if (!Enabled || Locked || (IsOpen && !CanClose)) return false;

        bool allowed = true;
        Interacting?.Invoke(player, this, ref allowed);
        if (!allowed) return false;

        bool authorized = KeycardPermissions == DoorPermissionFlags.None ||
                          Authorize is not null && Authorize.GetInvocationList().Cast<Func<Player, UsefulDoor, bool>>().Any(x => x(player, this));
        if (!IsOpen && !authorized) return false;
        IsOpen = !IsOpen;
        PlayState();
        MapAudio.Play(IsOpen ? "ElevatorOpen1.ogg" : "ElevatorClose1.ogg", "UsefulDoor", Position);
        MapSignals.RaiseDoorStateChanged(player, this, IsOpen);
        return true;
    }

    internal bool Matches(string tag) => !string.IsNullOrWhiteSpace(tag) && string.Equals(Tag, tag.Trim(), StringComparison.OrdinalIgnoreCase);

    private void PlayState()
    {
        try { Schematic?.AnimationController.Play(IsOpen ? "Open" : "Close"); } catch { }
    }
}
