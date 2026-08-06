using LabApi.Events.Arguments.PlayerEvents;
using Sliced.API.Features;

namespace AntiMeme.Changes;

/// <summary>
/// 参加した人へ出す最初の案内です。
/// </summary>
public sealed class JoinGreeting : EventHandlerBase
{
    private const ushort Duration = 6;

    /// <inheritdoc/>
    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        if (ev.Player is not { IsDestroyed: false, IsNpc: false } player) return;

        player.SendBroadcast(
            $"\n<size=28><color=#008cff>{AntiMemePlugin.Settings.ServerName}</color>へようこそ！\n" +
            "本サーバーはRP鯖です。RPを念頭に置いておく以外の制約は無いので自由に楽しんでください！</size>",
            Duration);
    }
}
