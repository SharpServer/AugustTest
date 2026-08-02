namespace Sliced.API.Enums;

/// <summary>
/// EventHandlerBase インスタンスが自動破棄されるタイミングを表します。
/// </summary>
public enum HandlerLifetime
{
    /// <summary>
    /// 自動破棄しません。Dispose() / DisposeAll() が呼ばれるまで生存します。
    /// プラグインの起動から終了まで生きるハンドラ用。
    /// </summary>
    Manual,

    /// <summary>
    /// ラウンド再開時 (LabApi ServerEvents.RoundRestarted) に自動で Dispose されます。
    /// ラウンド中だけ生きるハンドラ (CustomRole の能力など) 用。
    /// </summary>
    Round,
}
