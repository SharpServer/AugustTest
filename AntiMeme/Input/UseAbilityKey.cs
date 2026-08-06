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
/// 選択中のアビリティを発動します。
/// </summary>
public sealed class UseAbilityKey : InputBinding
{
    public override string Label => "アビリティ使用";

    public override string Hint => "選択中のアビリティを発動します。左 Alt を推奨します。";

    public override KeyCode DefaultKey => KeyCode.LeftAlt;

    public override string Group => "アビリティ操作";

    public override int Order => 10;

    public override void OnPressed(Player player)
    {
        if (AbilityBar.Selected(player) is not { } ability) return;

        if (!ability.TryUse(out string failureReason))
            player.SendHint(failureReason, 2f);
    }
}
