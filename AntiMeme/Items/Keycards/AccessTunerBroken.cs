using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>故障した診断装置です。データセルを当てるまで何もハックできません。</summary>
public sealed class AccessTunerBroken : AccessTuner
{
    /// <inheritdoc/>
    protected override int InitialLevel => 0;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.gray;
}
