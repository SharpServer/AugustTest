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
/// 複数のアビリティを持っているとき、次のものへ切り替えます。
/// </summary>
public sealed class SwitchAbilityKey : InputBinding
{
    public override string Label => "アビリティ切り替え";

    public override string Hint => "複数のアビリティを持っているとき、次のものへ切り替えます。中マウスボタンを推奨します。";

    public override KeyCode DefaultKey => KeyCode.Mouse2;

    public override string Group => "アビリティ操作";

    public override int Order => 11;

    public override void OnPressed(Player player)
    {
        if (AbilityBar.SelectNext(player) is { } ability)
            player.SendHint($"アビリティ: {ability.DisplayName}", 2f);
    }
}
