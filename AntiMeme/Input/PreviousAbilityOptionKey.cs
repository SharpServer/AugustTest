using System.Collections.Generic;
using System.Linq;
using AntiMeme.Abilities;
using AntiMeme.Audio;
using AntiMeme.Items.Weapons;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Input;

/// <summary>
/// 選択中のアビリティにオプションがある場合、前のオプションへ切り替えます。
/// </summary>
public sealed class PreviousAbilityOptionKey : InputBinding
{
    public override string Label => "アビリティオプション左";

    public override string Hint => "選択中のアビリティに複数のオプションがある場合、前へ切り替えます。左矢印キーを推奨します。";

    public override KeyCode DefaultKey => KeyCode.LeftArrow;

    public override string Group => "アビリティ操作";

    public override int Order => 13;

    public override void OnPressed(Player player)
    {
        if (AbilityBar.Selected(player) is not ChoiceAbility choice) return;

        choice.SelectPrevious();
        player.SendHint($"{choice.Name}: {choice.Selected.Name}", 2f);
    }
}
