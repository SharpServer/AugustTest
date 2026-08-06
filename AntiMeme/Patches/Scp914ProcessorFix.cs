using InventorySystem;
using InventorySystem.Items;
using Scp914.Processors;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Patches;

/// <summary>
/// すべてのアイテムが SCP-914 のイベントを通るようにします。
///
/// <para>
/// <c>Scp914Upgrader</c> は <c>TryGetProcessor</c> が失敗した時点でそのアイテムを飛ばすので、
/// <see cref="Scp914ItemProcessor"/> が付いていないプレハブは
/// <c>Scp914ProcessingInventoryItem</c> / <c>Scp914ProcessingPickup</c> すら発火しません。
/// つまり <c>AntiMeme.Items.Scp914</c> の変換規則を書いても、
/// そのアイテムがバニラの処理器を持っていなければ一切呼ばれません。
/// </para>
/// <para>
/// 足りないものに素通しの処理器を付けて、イベントだけは必ず飛ぶようにします。
/// 規則が無ければ入力と同じアイテムが出るので、見た目の挙動はバニラと変わりません。
/// </para>
/// </summary>
public sealed class Scp914ProcessorFix : EventHandlerBase
{
    public override void OnServerWaitingForPlayers() => AddMissingProcessors();

    /// <summary>
    /// ラウンド中にプラグインを読み込み直した場合にも効くよう、購読と同時に 1 回当てます。
    /// </summary>
    protected override void OnEnabled() => AddMissingProcessors();

    private static void AddMissingProcessors()
    {
        int added = 0;

        foreach (var entry in InventoryItemLoader.AvailableItems)
        {
            ItemBase item = entry.Value;

            if (item == null || item.TryGetComponent(out Scp914ItemProcessor _)) continue;

            ItemType[] outputs = [entry.Key];
            StandardItemProcessor processor = item.gameObject.AddComponent<StandardItemProcessor>();

            processor._roughOutputs = outputs;
            processor._coarseOutputs = outputs;
            processor._oneToOneOutputs = outputs;
            processor._fineOutputs = outputs;
            processor._veryFineOutputs = outputs;
            processor._fireUpgradeTrigger = false;

            added++;
        }

        if (added > 0)
            Logger.Debug($"[Scp914] 処理器の無いアイテム {added} 件に素通しの処理器を足しました。");
    }
}
