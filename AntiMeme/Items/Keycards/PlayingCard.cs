using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>権限を一切持たない Joker のトランプです。</summary>
public sealed class PlayingCard : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<Utility.Quarter>(8),
        OneToOne = Scp914Rule.To<MasterCard>(),
        Fine = Scp914Rule.To<KeycardSupplyManager>(),
        VeryFine = Scp914Rule.To<KeycardSurveillance>(),
    };

    public override string Name => "Playing Card";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;
    public override string Description => "ただのトランプ。何にも使えない";

    protected override string Label => "Playing Card";
    protected override Color32 LabelColor => new(255, 255, 255, 255);
    protected override string HolderName => "Role. Joker";
    protected override Color32 Tint => new(0, 0, 0, 255);
    protected override Color32 PermissionsColor => new(0, 0, 0, 255);
    protected override KeycardLevels Levels => default;
}
