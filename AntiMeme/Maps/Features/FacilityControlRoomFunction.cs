using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;

namespace AntiMeme.Maps.Features;

/// <summary>
/// 施設管制室のコンソールから実行できる機能 1 つ分です。
///
/// <para>
/// 継承したクラスは <see cref="FacilityControlRoom"/> が自動で拾います。登録も ID もありません。
/// </para>
/// </summary>
public abstract class FacilityControlRoomFunction
{
    /// <summary>コンソールに出す名前です。</summary>
    public abstract string DisplayName { get; }

    /// <summary>コンソールに出す説明です。</summary>
    public abstract string Description { get; }

    /// <summary>並び順です。小さいものが先に出ます。</summary>
    public virtual int Order => 0;

    /// <summary>実行に必要なキーカード権限です。</summary>
    public virtual DoorPermissionFlags RequiredPermissions => DoorPermissionFlags.None;

    /// <summary>実行後のクールダウン秒数です。0 以下ならクールダウンなし。</summary>
    public virtual float Cooldown => 0f;

    /// <summary>1 ラウンドあたりの実行上限です。0 以下なら無制限。</summary>
    public virtual int MaxExecutions => 0;

    /// <summary>ラウンド境界で状態を戻します。</summary>
    public virtual void ResetState()
    {
    }

    /// <summary>機能の中身です。</summary>
    public abstract FacilityControlRoomFunctionResult Execute(FacilityControlRoomFunctionContext context);

    /// <summary>成功。実行回数に数えます。</summary>
    protected static FacilityControlRoomFunctionResult Success(string hint) =>
        new FacilityControlRoomFunctionResult(hint, true);

    /// <summary>失敗。実行回数に数えません。</summary>
    protected static FacilityControlRoomFunctionResult Failure(string hint) =>
        new FacilityControlRoomFunctionResult(hint, false);
}

/// <summary>機能を実行したときの状況です。</summary>
public readonly struct FacilityControlRoomFunctionContext
{
    public FacilityControlRoomFunctionContext(Player player, KeycardItem keycard, int executedCount)
    {
        Player = player;
        Keycard = keycard;
        ExecutedCount = executedCount;
    }

    /// <summary>操作しているプレイヤーです。</summary>
    public Player Player { get; }

    /// <summary>コンソールに差してあるキーカードです。</summary>
    public KeycardItem Keycard { get; }

    /// <summary>この機能をこのラウンドで既に実行した回数です。</summary>
    public int ExecutedCount { get; }
}

/// <summary>機能の実行結果です。</summary>
public readonly struct FacilityControlRoomFunctionResult
{
    public FacilityControlRoomFunctionResult(string hint, bool countAsExecution)
    {
        Hint = hint ?? string.Empty;
        CountAsExecution = countAsExecution;
    }

    /// <summary>操作者へ返す文言です。</summary>
    public string Hint { get; }

    /// <summary>実行回数とクールダウンに数えるか。</summary>
    public bool CountAsExecution { get; }
}
