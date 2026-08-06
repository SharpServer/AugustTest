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
/// 近接チャットを切り替えます。
/// </summary>
public sealed class ProximityChatKey : InputBinding
{
    public override string Label => "近接チャット";

    public override string Hint => "一部の役職で、SCP チャットを近くの相手にも聞こえるようにします。V を推奨します。";

    public override KeyCode DefaultKey => KeyCode.V;

    public override string Group => "通信";

    public override int Order => 20;

    public override void OnPressed(Player player)
    {
        if (!ProximityVoice.CanUse(player)) return;

        bool enabled = ProximityVoice.Toggle(player);
        player.SendHint(enabled ? "近接チャット: ON" : "近接チャット: OFF", 2f);
    }
}
