using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Melee;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Abilities;

/// <summary>SCP-076 が近接武器を具現化する能力です。</summary>
public sealed class GenerateWeaponAbility : ChoiceAbility
{
    private static readonly IReadOnlyList<AbilityChoice> WeaponChoices =
    [
        new GenerateWeaponChoice<BattleAxe>("バトルアックス"),
        new GenerateWeaponChoice<ThrowKnife>("投げナイフ"),
    ];

    public override string Name => "武器の錬成";

    public override float Cooldown => 120f;

    public override IReadOnlyList<AbilityChoice> Choices => WeaponChoices;
}

internal sealed class GenerateWeaponChoice<T> : AbilityChoice where T : CustomItem, new()
{
    public GenerateWeaponChoice(string name)
    {
        Name = name;
    }

    public override string Name { get; }

    public override string Description => $"{Name}を生成します。";

    public override bool CanUse(Player player, out string failureReason)
    {
        if (player.IsInventoryFull)
        {
            failureReason = "インベントリが満杯です。";
            return false;
        }

        if (player.Items.Any(item => CustomItem.Of(item.Serial) is T))
        {
            failureReason = "既にこのアイテムを所持しています。";
            return false;
        }

        failureReason = null;
        return true;
    }

    public override void Use(Player player) => CustomItem.Give<T>(player);
}
