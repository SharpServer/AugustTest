using AntiMeme.Effects;
using AntiMeme.Items.Bases;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using Sliced.API.Features.Attributes;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// クラス Z-記憶補強剤。反ミーム性の現象を完全に無効化しますが、代償として
/// SCP-207 と同じ負荷が体に残り続けます。
/// </summary>
[LegacyName("ClassZMemoryForcePil")]
public sealed class ClassZMemoryForcePill : CustomUsable
{
    private const float StaminaDuration = 60f;
    private const byte ColaIntensity = 4;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SCP500;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(185, 75, 255, 255);

    /// <inheritdoc/>
    public override string Name => "クラスZ-記憶補強剤";

    /// <inheritdoc/>
    public override string Description =>
        "反ミーム性の現象等に対抗するために使用される強力な薬。\n反ミームの影響を無効化する\n" +
        "効果時間：---\n注意書き：<color=red>とても危険です！使用を控えるべきです！</color>";

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
        player.EnableEffect<Invigorated>(1, StaminaDuration);
        player.EnableEffect<Scp207>(ColaIntensity);

        // 効果時間を指定しなければ永続。代償の SCP-207 も切れない。
        AntiMemeResistance.Give(player);
    }
}
