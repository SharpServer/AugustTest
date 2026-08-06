using System;
using System.Linq;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>カード権限を外部判定へ委譲できる簡易認証扉です。</summary>
public sealed class AuthDoor : ObjectPrefab
{
    public const string CentralSchematicName = "MovingDoor";
    public DoorPermissionFlags RequiredPermissions { get; set; } = DoorPermissionFlags.None;
    /// <summary>旧マーカー名との互換エイリアスです。</summary>
    public DoorPermissionFlags KeycardPermissions { get => RequiredPermissions; set => RequiredPermissions = value; }
    public bool RequireAllPermissions { get; set; } = true;
    public bool CanClose { get; set; }
    public float TransitionDuration { get; set; } = 1f;
    public bool IsOpen { get; private set; }
    public bool IsTransitioning { get; private set; }
    public static event Func<Player, AuthDoor, bool> Authorize;
    protected override string SchematicName => CentralSchematicName;

    protected override void OnSetup()
    {
        InteractableHandle handle = GetInteractable("Interactable") ?? AddInteractable(0f, offset: Vector3.up * .75f, scale: Vector3.one * 1.2f);
        handle.Interacted += HandleInteracted;
        PlayState(false);
    }

    private void HandleInteracted(PlayerSearchedToyEventArgs ev)
    {
        if (ev?.Player is not { IsDestroyed: false } player || IsTransitioning) return;
        if (IsOpen)
        {
            if (CanClose) SetState(player, false);
            return;
        }
        bool authorized = RequiredPermissions == DoorPermissionFlags.None ||
                          Authorize is not null && Authorize.GetInvocationList().Cast<Func<Player, AuthDoor, bool>>().Any(x => x(player, this));
        if (!authorized)
        {
            player.SendHint("<color=yellow>権限が足りないようだ</color>", 3f);
            return;
        }
        SetState(player, true);
    }

    private void SetState(Player player, bool open)
    {
        IsOpen = open;
        IsTransitioning = true;
        string state = open ? "door1" : "door3";
        try { Schematic?.AnimationController.Play(state); } catch { }
        MapAudio.Play(open ? "ElevatorOpen1.ogg" : "ElevatorClose1.ogg", "AuthDoor", Position);
        MapSignals.RaiseDoorStateChanged(player, this, open);
        Animator animator = Schematic?.AnimationController?.Animators?.FirstOrDefault();
        WaitForAnimatorState(animator, state, TransitionDuration, () => IsTransitioning = false);
    }

    private void PlayState(bool open)
    {
        try { Schematic?.AnimationController.Play(open ? "door2" : "door0"); } catch { }
    }
}
