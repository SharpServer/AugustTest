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
/// 手に持っている Hybrid アイテムのモードを切り替えます。
/// </summary>
public sealed class SwitchItemModeKey : InputBinding
{
    public override string Label => "アイテムモード切り替え";

    public override string Hint => "複数のモードを持つアイテムを切り替えます。G を推奨します。";

    public override KeyCode DefaultKey => KeyCode.G;

    public override string Group => "アイテム操作";

    public override int Order => 30;

    public override void OnPressed(Player player)
    {
        if (player.CurrentItem is not { } held) return;

        if (CustomItem.Of(held) is HybridWeapon hybrid)
            hybrid.SwitchMode();
    }
}
