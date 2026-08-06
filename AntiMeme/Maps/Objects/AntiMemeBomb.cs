using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>操作時に全参加者へ爆発通知を送り、自身を破棄する演出爆弾です。</summary>
public sealed class AntiMemeBomb : ObjectPrefab
{
    public float InteractionDuration { get; set; } = 5f;
    public bool Armed { get; private set; }
    public static event Action<Vector3> Detonated;
    protected override string SchematicName => "AntiMemeBomb";
    protected override void OnSetup()
    {
        InteractableHandle handle = GetInteractable("Interactable") ?? AddInteractable(InteractionDuration, offset: Vector3.up * 2.05f, scale: Vector3.one * 3f);
        handle.Interacted += HandleInteracted;
    }
    private void HandleInteracted(PlayerSearchedToyEventArgs ev)
    {
        if (Armed) return;
        Armed = true;
        Vector3 location = Schematic?.Position ?? Position;
        foreach (Player player in Player.ReadyList.Where(player => player is { IsDestroyed: false, IsAlive: true }))
            player.Damage(100000f, "AntiMeme Bomb");
        Detonated?.Invoke(location);
        Destroy();
    }
}
