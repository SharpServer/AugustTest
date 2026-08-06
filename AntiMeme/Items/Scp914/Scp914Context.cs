using LabApi.Features.Wrappers;
using Scp914;
using UnityEngine;

namespace AntiMeme.Items.Scp914;

/// <summary>
/// SCP-914 のルールを実行するときに渡される状況です。
///
/// 床のアイテムなら <see cref="Pickup"/>、インベントリのアイテムなら
/// <see cref="Item"/> と <see cref="Owner"/> が入ります。
/// </summary>
public readonly struct Scp914Context
{
    public Scp914Context(Scp914KnobSetting setting, Pickup pickup, Vector3 outputPosition)
    {
        Setting = setting;
        Pickup = pickup;
        Item = null;
        Owner = null;
        OutputPosition = outputPosition;
    }

    public Scp914Context(Scp914KnobSetting setting, Item item, Player owner)
    {
        Setting = setting;
        Pickup = null;
        Item = item;
        Owner = owner;
        OutputPosition = owner.Position;
    }

    /// <summary>
    /// つまみの設定です。
    /// </summary>
    public Scp914KnobSetting Setting { get; }

    /// <summary>
    /// 床にあったアイテムです。インベントリ由来なら null。
    /// </summary>
    public Pickup Pickup { get; }

    /// <summary>
    /// インベントリにあったアイテムです。床由来なら null。
    /// </summary>
    public Item Item { get; }

    /// <summary>
    /// インベントリの持ち主です。床由来なら null。
    /// </summary>
    public Player Owner { get; }

    /// <summary>
    /// 出力側の位置です。
    /// </summary>
    public Vector3 OutputPosition { get; }

    /// <summary>
    /// インベントリ由来かどうか。
    /// </summary>
    public bool IsInventory => Item is not null;

    /// <summary>
    /// 対象アイテムのシリアルです。
    /// </summary>
    public ushort Serial => IsInventory ? Item.Serial : Pickup.Serial;

    /// <summary>
    /// 元のアイテムを消します。
    /// </summary>
    public void Consume()
    {
        if (IsInventory)
            Owner.RemoveItem(Item);
        else
            Pickup.Destroy();
    }

    /// <summary>
    /// 出力を渡します。インベントリ由来なら持ち主へ、床由来なら出力位置へ落とします。
    /// インベントリが満杯なら足元に落とします。
    /// </summary>
    public void Deliver(ItemType type)
    {
        if (IsInventory && !Owner.IsInventoryFull)
        {
            Owner.AddItem(type);

            return;
        }

        Pickup.Create(type, OutputPosition)?.Spawn();
    }
}
