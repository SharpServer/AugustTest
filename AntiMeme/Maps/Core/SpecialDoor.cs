using System;
using System.Linq;
using AntiMeme.Input;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Maps.Core;

/// <summary>
/// マップ側で用意した「普通のキーカードでは開かない扉」1 枚ぶんです。
///
/// <para>
/// 継承したクラスは <see cref="SpecialDoorHandler"/> が自動で拾います。登録も一覧も要りません。
/// 扉そのものはマップ (ProjectMER) 側のマーカー位置で特定します。
/// </para>
/// </summary>
/// <example>
/// <code>
/// public sealed class ObservationDoorO1 : SpecialDoor
/// {
///     public override string Marker => "CDoor_O1";
///     public override string Code   => "1217";
/// }
/// </code>
/// </example>
public abstract class SpecialDoor
{
    /// <summary>扉の位置とみなす距離です。マーカーと扉の原点が完全一致しないため幅を持たせます。</summary>
    public const float PositionTolerance = 0.75f;

    /// <summary>この扉を指すマップマーカーの名前です。</summary>
    public abstract string Marker { get; }

    /// <summary>開けるのに必要なパスコードです。null なら番号は不要。</summary>
    public virtual string Code => null;

    /// <summary>開けるのに必要なカスタムアイテムの型です。null ならアイテムは不要。</summary>
    public virtual Type RequiredItem => null;

    /// <summary>この扉の現在位置です。マーカーが無ければ null。</summary>
    public Vector3? Position =>
        MapPoints.TryGet(Marker, out Vector3 position) ? position : null;

    /// <summary>
    /// この扉が <paramref name="position"/> にあるか。
    /// </summary>
    public bool IsAt(Vector3 position) =>
        Position is { } own && (own - position).sqrMagnitude <= PositionTolerance * PositionTolerance;

    /// <summary>
    /// 開けてよいか。番号とアイテムの両方を設定した場合は<b>どちらか</b>を満たせば開きます。
    /// </summary>
    public virtual bool CanOpen(Player player)
    {
        if (Code is null && RequiredItem is null) return false;

        return (Code is not null && Passcode.Has(player, Code)) ||
               (RequiredItem is not null && HasRequiredItem(player));
    }

    /// <summary>開けられなかったときに出す文言です。</summary>
    public virtual string DeniedHint =>
        (Code, RequiredItem) switch
        {
            (not null, not null) => "<size=24>専用のアクセス用アイテム、またはコードが必要そうだ・・・</size>",
            (not null, null) => "<size=24>コードが正しくないようだ・・・</size>",
            (null, not null) => "<size=24>専用のアクセス用アイテムが必要そうだ・・・</size>",
            _ => "<size=24>しかし、何も反応しなかった。</size>",
        };

    private bool HasRequiredItem(Player player) =>
        player.Items.Any(item => CustomItem.Of(item.Serial)?.GetType() == RequiredItem);
}
