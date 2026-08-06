using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>低い権限の扉をハックできる診断装置です。</summary>
public sealed class AccessTunerLv1 : AccessTuner, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        OneToOne = Scp914Rule.To<KeycardChaosIntruder>(),
    };

    /// <inheritdoc/>
    protected override int InitialLevel => 1;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;
}
