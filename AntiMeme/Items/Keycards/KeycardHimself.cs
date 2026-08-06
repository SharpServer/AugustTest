using AntiMeme.Items.Bases;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 死体が持っていた「なぞのキーカード」です。中身は主席研究員のカードと同じで、
/// 所持者名だけが違います。
/// </summary>
public sealed class KeycardHimself : KeycardHeadResearcher
{
    /// <inheritdoc/>
    public override string Name => "なぞのキーカード";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.45f, 0.65f, 1f);

    /// <inheritdoc/>
    public override string Description =>
        "死体が持っていたと思われるなぞのキーカード\n" +
        "<color=yellow>独自の認証チップが埋め込まれている・・・？</color>";

    /// <inheritdoc/>
    protected override string HolderName => "Hrs. Redheart";
}
