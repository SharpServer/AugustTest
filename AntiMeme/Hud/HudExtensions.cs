using LabApi.Features.Wrappers;

namespace AntiMeme.Hud;

/// <summary>
/// HUD の入口です。<c>using AntiMeme.Hud;</c> だけで使えます。
/// </summary>
public static class HudExtensions
{
    /// <summary>
    /// このプレイヤーの HUD を取り出します。
    /// </summary>
    public static PlayerHud Hud(this Player player) => new PlayerHud(player);

    /// <summary>
    /// 一時的な通知を出します。
    ///
    /// HintServiceMeow の互換レイヤーが <see cref="Player.SendHint(string, float)"/> を
    /// 受け取って HUD と衝突しない位置に並べ直すので、呼び出し側はこのままで構いません。
    /// ここは「後から出し方の方針を変えるときに 1 か所で済ませる」ための入口です。
    /// </summary>
    public static void Notify(this Player player, string text, float duration = 3f) =>
        player.SendHint(text, duration);

    /// <summary>
    /// 全員に一時的な通知を出します。
    /// </summary>
    public static void NotifyAll(string text, float duration = 3f)
    {
        foreach (Player player in Player.ReadyList)
        {
            player.SendHint(text, duration);
        }
    }
}
