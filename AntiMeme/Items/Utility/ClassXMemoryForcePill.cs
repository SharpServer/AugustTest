using AntiMeme.Effects;
using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using Sliced.API.Features.Attributes;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// クラス X-記憶補強剤。反ミーム性の現象への耐性を一時的に得ます。
/// </summary>
[LegacyName("ClassXMemoryForcePil")]
public sealed class ClassXMemoryForcePill : CustomUsable, IScp914Upgradable
{
    private const float Duration = 60f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SCP500;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;

    /// <inheritdoc/>
    public override string Name => "クラスX-記憶補強剤";

    /// <inheritdoc/>
    public override string Description =>
        "反ミーム性の現象等に対抗するために使用される薬。\n反ミームの影響を軽減する。\n効果時間：1分";

    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.SCP500),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.To<ClassZMemoryForcePill>(),
        VeryFine = Scp914Rule.To<ClassZMemoryForcePill>(),
    };

    /// <inheritdoc/>
    protected override bool CanUse(Player player)
    {
        if (!AntiMemeResistance.Has(player)) return true;

        player.SendHint("既に耐性を得ている為、使用できません。", 3f);

        return false;
    }

    /// <inheritdoc/>
    protected override void OnUse(Player player)
    {
        player.EnableEffect<Invigorated>(1, Duration);
        AntiMemeResistance.Give(player, Duration);
    }
}
