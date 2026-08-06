using AntiMeme.Items.Bases;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// Advanced Medkit。体力を全快させ、そのうえ AHP を少し置いていくメディキットです。
/// </summary>
public sealed class AdvancedMedkit : CustomUsable
{
    private const float ArtificialHealth = 15f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Medkit;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.green;

    /// <inheritdoc/>
    public override string Name => "Advanced Medkit";

    /// <inheritdoc/>
    public override string Description => "重篤な負傷も手当てできるよう拡張された、高度な医療キット。";

    /// <inheritdoc/>
    protected override void OnUse(Player player)
    {
        player.Heal(player.MaxHealth);
        AddArtificialHealth(player, ArtificialHealth);
    }
}
