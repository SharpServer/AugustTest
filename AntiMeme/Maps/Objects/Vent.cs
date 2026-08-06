using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>名札付きトリガーポイントへ移動する通風孔です。</summary>
public sealed class Vent : ObjectPrefab
{
    public string ExitPointTag { get; set; } = string.Empty;
    public float InteractionDuration { get; set; } = 1.5f;
    protected override string SchematicName => "Vent";

    protected override void OnSetup()
    {
        InteractableHandle handle = GetInteractable("Interactable") ?? AddInteractable(InteractionDuration);
        handle.Interacted += HandleInteracted;
    }

    private void HandleInteracted(PlayerSearchedToyEventArgs ev)
    {
        if (ev?.Player is not { IsDestroyed: false } player || !MapPoints.TryGet(ExitPointTag, out Vector3 destination, out Quaternion rotation)) return;
        MapAudio.Play("ventsound.ogg", "Vent", Position, maxDistance: 10f);
        player.Position = destination;
        player.Rotation = rotation;
        MapAudio.Play("ventsound.ogg", "VentExit", destination, maxDistance: 10f);
    }
}
