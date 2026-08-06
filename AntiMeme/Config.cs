using System.ComponentModel;
using Exiled.API.Interfaces;

namespace AntiMeme;

/// <summary>
/// %APPDATA%\EXILED\Configs\Plugins\AntiMeme\&lt;port&gt;.yml に出る設定です。
///
/// ここに足してよいのは「サーバー運営者が実行中に変えたい値」だけです。
/// 1 つのアイテム / 役職 / 機能でしか使わない値は、その所有クラスの
/// const / static readonly に置いてください。
/// 旧 Slafight でこの規律が効いていたので、そのまま引き継ぎます。
/// </summary>
public class Config : IConfig
{
    [Description("Whether this plugin is enabled.")]
    public bool IsEnabled { get; set; } = true;

    [Description("Whether debug messages should be shown in the console.")]
    public bool Debug { get; set; }

    [Description("ベータ運用中かどうか。HUD 上部のサーバー名に BETA 表記を添えます。")]
    public bool IsBeta { get; set; }

    [Description("季節。装飾・キャンディの中身・戦士陣営の呼称が切り替わります。" +
                 "None / April / FifthFestival / Summer / Halloween / Christmas")]
    public Season Season { get; set; } = Season.None;

    [Description("サーバー名。Discord への通知に添えます。")]
    public string ServerName { get; set; } = "シャープ鯖";

    [Description("Discord Bot 連携 (bot.py) の Flask API エンドポイント。通常は変更不要です。")]
    public string DiscordBotApiUrl { get; set; } = "http://localhost:5000";

    [Description("Discord Bot 連携の API 認証用シークレット。bot.py 側の API_SECRET と同じ値にしてください。" +
                 "空のままだと Discord 連携は動きません。")]
    public string DiscordBotApiSecret { get; set; } = string.Empty;
}
