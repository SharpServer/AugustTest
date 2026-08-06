using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>使用するたびに透明状態を切り替えるホワイトスーツ用装置です。</summary>
public sealed class CloakGenerator : CustomUsable, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.ToVanilla(ItemType.SCP268),
        Coarse = Scp914Rule.ToVanilla(ItemType.SCP268),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.Destroy,
        VeryFine = Scp914Rule.Destroy,
    };

    public override ItemType BaseType => ItemType.SCP268;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.white;

    public override string Name => "外套ジェネレータ";

    public override string Description =>
        "ホワイトスーツが持つ特殊な透明化装置。\n使用することで透明・非透明を切り替えられる";

    protected override int MaxUses => 0;

    protected override bool DestroyWhenDepleted => false;

    protected override void OnUse(Player player)
    {
        if (player.ReferenceHub.playerEffectsController.TryGetEffect(out Invisible invisible) && invisible.IsEnabled)
            player.DisableEffect<Invisible>();
        else
            player.EnableEffect<Invisible>();
    }
}
