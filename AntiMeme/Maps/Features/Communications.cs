using System;
using LabApi.Features.Wrappers;

namespace AntiMeme.Maps.Features;

/// <summary>マップ通信端末の入力を音声/ゲームモード層へ委譲します。</summary>
public sealed class Communications : MapFeature
{
    public static event Action<Player, string> MessageSent;
    public static bool TrySend(Player player, string message)
    {
        if (player is not { IsDestroyed: false } || string.IsNullOrWhiteSpace(message)) return false;
        MessageSent?.Invoke(player, message.Trim());
        return true;
    }
}
