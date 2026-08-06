using AntiMeme.Items.Bases;
using CustomPlayerEffects;
using CustomRendering;
using LabApi.Features.Wrappers;
using Sliced.API.Features.Attributes;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>炎上フラミンゴ視界を付与する安全化済み LS-D5 です。</summary>
[LegacyName("LsdPill")]
public sealed class LsdPillSafe : CustomUsable
{
    public override ItemType BaseType => ItemType.Adrenaline;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.gray;

    public override string Name => "LS-D5";

    public override string Description => "その辺に落ちていた薬を改良して安全にハイになれるようにした薬。";

    protected override bool CancelVanillaUse => false;

    protected override void OnUse(Player player)
    {
        player.EnableEffect<FogControl>();
        if (player.ReferenceHub.playerEffectsController.TryGetEffect(out FogControl fog))
            fog.SetFogType(FogType.BecomingFlamingo);
    }
}
