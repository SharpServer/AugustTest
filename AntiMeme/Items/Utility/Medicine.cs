using AntiMeme.Items.Bases;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// 注射器型メディキット。アドレナリンの見た目で、一発で立て直せる即時医療薬です。
/// </summary>
public sealed class Medicine : CustomUsable
{
    private const float HealAmount = 75f;
    private const float ArtificialHealth = 60f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Adrenaline;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;

    /// <inheritdoc/>
    protected override string PickupModel => "Alienisolation_medkit";

    /// <inheritdoc/>
    public override string Name => "注射器型メディキット";

    /// <inheritdoc/>
    public override string Description =>
        "セラムをベースに、医務室のニーズに合わせて開発された即時医療薬。\n" +
        "激務の財団職員をだいたい一発で治せる優れモノ。";

    /// <inheritdoc/>
    protected override void OnUse(Player player)
    {
        player.Heal(HealAmount);
        AddArtificialHealth(player, ArtificialHealth);
    }
}
