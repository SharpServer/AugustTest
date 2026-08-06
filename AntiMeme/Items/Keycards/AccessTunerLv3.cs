using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>ほぼ全ての扉をハックできる診断装置です。</summary>
public sealed class AccessTunerLv3 : AccessTuner
{
    /// <inheritdoc/>
    protected override int InitialLevel => 3;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.red;
}
