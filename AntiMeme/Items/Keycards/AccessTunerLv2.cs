using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>ある程度の権限の扉をハックできる診断装置です。</summary>
public sealed class AccessTunerLv2 : AccessTuner
{
    /// <inheritdoc/>
    protected override int InitialLevel => 2;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(255, 153, 102, 255);
}
