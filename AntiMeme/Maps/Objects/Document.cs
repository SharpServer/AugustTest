using System;
using System.Collections.Generic;
using AntiMeme.Maps.Features;
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>読み取り時に本文を表示する ProjectMER 文書です。</summary>
public sealed class Document : ObjectPrefab
{
    private const string DefaultSchematic = "Document";
    private const float DefaultDuration = 3f;
    private string modelSchematicName = DefaultSchematic;
    private bool showModel = true;
    private float interactionDuration = DefaultDuration;

    public DocumentType DocumentType { get; set; } = DocumentType.Scp033;

    /// <summary>旧マーカーの SchematicName と現行 ModelSchematicName の両方を受け付けます。</summary>
    public string ModelSchematicName
    {
        get => modelSchematicName;
        set
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(modelSchematicName, normalized, StringComparison.Ordinal)) return;
            modelSchematicName = normalized;
            RefreshModel();
        }
    }

    public bool ShowModel
    {
        get => showModel;
        set
        {
            if (showModel == value) return;
            showModel = value;
            RefreshModel();
        }
    }

    public float InteractionDuration
    {
        get => interactionDuration;
        set => interactionDuration = float.IsNaN(value) || float.IsInfinity(value) ? DefaultDuration : Mathf.Max(0f, value);
    }

    protected override string SchematicName => showModel && !string.IsNullOrWhiteSpace(modelSchematicName) ? modelSchematicName : null;

    protected override void OnSetup()
    {
        InteractableHandle handle = AddInteractable(interactionDuration, scale: Vector3.one * 0.75f);
        handle.Interacted += OnRead;
    }

    public override void ApplyOptions(IReadOnlyDictionary<string, string> options)
    {
        if (options is null || options.Count == 0) return;

        Dictionary<string, string> normalized = new Dictionary<string, string>(options, StringComparer.OrdinalIgnoreCase);
        if (normalized.TryGetValue("SchematicName", out string schematic) && !normalized.ContainsKey(nameof(ModelSchematicName)))
            normalized[nameof(ModelSchematicName)] = schematic;
        base.ApplyOptions(normalized);
    }

    private void OnRead(PlayerSearchedToyEventArgs ev)
    {
        if (ev?.Player is not { IsDestroyed: false } player) return;
        string text = DocumentDictionary.Get(DocumentType);
        player.SendHint(text, 8f);
        MapAudio.Play("PickItem0.ogg", "Document", Schematic?.Position ?? Position, maxDistance: 10f);
        MapSignals.RaiseDocumentRead(player, DocumentType.ToString(), Schematic?.Position ?? Position);
    }

    private void RefreshModel()
    {
        if (!IsAlive) return;
        if (SchematicName is { Length: > 0 } name) SpawnSchematic(name);
        else DespawnSchematic();
    }
}
