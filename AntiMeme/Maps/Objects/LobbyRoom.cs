using System.Linq;
using AdminToys;
using ProjectMER.Features.Objects;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace AntiMeme.Maps.Objects;

/// <summary>
/// ラウンド開始待ちのロビー部屋です。人数・次イベント・残り時間の 3 枚の看板を持ちます。
/// </summary>
/// <remarks>
/// <para>
/// 進行そのものは <see cref="Features.Lobby"/> が持ちます。この型は看板の口だけを開けます。
/// </para>
/// <para>
/// <b>スキマティックは自分で出しません。</b> <c>OldMenuRoom</c> はマップ (ProjectMER) 側が
/// 置いている前提で、シーン上の実体を掴みに行きます。ここで <c>SchematicName</c> を宣言すると
/// マップの分と合わせて 2 つ出てしまいます。
/// </para>
/// <para>
/// 看板のブロックには <c>ObjectPrefabKey</c> が付いていないので、
/// <c>GetBlock</c> ではなく名前で引きます。
/// </para>
/// </remarks>
public sealed class LobbyRoom : ObjectPrefab
{
    /// <summary>マップが置いているスキマティックの名前です。</summary>
    public const string MenuSchematicName = "OldMenuRoom";

    private SchematicObject menu;

    /// <inheritdoc/>
    protected override float SetupDelay => 0f;

    /// <summary>接続人数を出す看板です。</summary>
    public TextToy PlayerCountText => TextOf("PlayerCountText");

    /// <summary>次のゲームモード名を出す看板です。</summary>
    public TextToy NextEventText => TextOf("NextEventText");

    /// <summary>開始までの残り秒数を出す看板です。</summary>
    public TextToy RemainingTimeText => TextOf("RemainingTimeText");

    /// <summary>3 枚とも掴めているかどうか。マップの読み込みが終わるまでは false です。</summary>
    public bool HasSigns =>
        PlayerCountText != null && NextEventText != null && RemainingTimeText != null;

    private TextToy TextOf(string blockName)
    {
        if (menu == null)
        {
            menu = UnityObject
                .FindObjectsByType<SchematicObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(schematic => schematic.Name == MenuSchematicName);
        }

        if (menu == null) return null;

        // 名前で引く。ProjectMER の allowPartial は既定 true なので明示的に切る。
        return menu.FindBlock(blockName, allowPartial: false) is { } block
            ? block.GetComponent<TextToy>()
            : null;
    }
}
