using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Net;

/// <summary>
/// Discord Bot (bot.py の Flask エンドポイント) へ通知を送ります。
///
/// <para>
/// 送信は fire-and-forget です。Discord 側が落ちていてもゲームは止めません。
/// シークレットが未設定なら何も送りません (設定し忘れで通信を試み続けないため)。
/// </para>
/// </summary>
public static class Discord
{
    private static readonly HttpClient Client = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    /// <summary>
    /// 連携が有効かどうか。シークレット未設定なら無効です。
    /// </summary>
    public static bool IsConfigured => !string.IsNullOrWhiteSpace(AntiMemePlugin.Settings.DiscordBotApiSecret);

    /// <summary>
    /// モデレーションイベントを通知します。
    /// </summary>
    /// <param name="type">種別 (<c>kick</c> / <c>ban</c> / <c>report_cheater</c> / <c>report_local</c> / <c>friendly_fire</c> など)。</param>
    /// <param name="data">JSON にできる任意のデータ。</param>
    public static void SendModeration(string type, object data) => Post("moderation_event", new
    {
        type,
        server = AntiMemePlugin.Settings.ServerName,
        timestamp = DateTime.UtcNow,
        data,
    });

    /// <summary>
    /// 現在の人数を通知します。
    /// </summary>
    public static void SendPlayerCount(int count) => Post("playercount", new
    {
        server = AntiMemePlugin.Settings.ServerName,
        timestamp = DateTime.UtcNow,
        count,
    });

    private static void Post(string endpoint, object payload)
    {
        if (!IsConfigured) return;

        _ = PostAsync(endpoint, payload);
    }

    private static async Task PostAsync(string endpoint, object payload)
    {
        // 外部 I/O なので、ここは落ちても呼び出し元へ伝播させない。
        try
        {
            Config config = AntiMemePlugin.Settings;
            string url = $"{config.DiscordBotApiUrl.TrimEnd('/')}/{endpoint}";

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };

            request.Headers.Add("X-Api-Key", config.DiscordBotApiSecret);

            await Client.SendAsync(request);
        }
        catch (TaskCanceledException exception)
        {
            Logger.Debug($"[AntiMeme] Discord への {endpoint} 送信がタイムアウトしました: {exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            Logger.Debug($"[AntiMeme] Discord への {endpoint} 送信に失敗しました: {exception.Message}");
        }
        catch (Exception exception)
        {
            Logger.Error($"[AntiMeme] Discord への {endpoint} 送信でエラーが発生しました: {exception}");
        }
    }
}
