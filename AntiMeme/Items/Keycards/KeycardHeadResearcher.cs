using AntiMeme.Items.Bases;
using Sliced.API.Features.Attributes;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// サイト-02 の主席研究員に配布されるキーカードです。発電機の権限判定を無視して解錠できます。
/// </summary>
[LegacyName("KeycardHeadResearcherGeneric")]
public class KeycardHeadResearcher : CustomKeycard
{
    /// <inheritdoc/>
    public override string Name => "主席研究員キーカード";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.45f, 0.65f, 1f);

    /// <inheritdoc/>
    public override string Description =>
        "サイト-02の高位な有数の科学者にしか配布されないキーカード。\n" +
        "様々なものにアクセスできる。\n<color=yellow>発電機の権限を無視して開けられる</color>";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(3, 1, 1);

    /// <inheritdoc/>
    protected override string Label => "HEAD RESEARCHER";

    /// <inheritdoc/>
    protected override Color32 LabelColor => new Color32(238, 246, 255, 255);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(255, 0, 0, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(0, 0, 0, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Hrs. Site-02";

    /// <inheritdoc/>
    protected override void OnUnlockingGenerator(PlayerUnlockingGeneratorEventArgs ev) => ev.CanOpen = true;
}
