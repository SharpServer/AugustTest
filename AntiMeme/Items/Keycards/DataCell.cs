using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// <see cref="AccessTuner"/> を強化・充填するデータセルです。
/// 拾おうとした瞬間に使われ、インベントリには入りません。
/// </summary>
public abstract class DataCell : CustomItem
{
    private static bool hooked;

    protected DataCell() => Hook();

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Ammo44cal;

    /// <inheritdoc/>
    public override string Name => $"Data Cell Level-{CellLevel}";

    /// <inheritdoc/>
    public override string Description => $"Access Tuner Lv.{CellLevel} と同期するデータセル。";

    /// <summary>
    /// このセルが同期させるレベルです。
    /// </summary>
    protected abstract int CellLevel { get; }

    /// <inheritdoc/>
    protected override string PickupModel => "Alienisolation_Datacell_lv1";

    private void Use(Player player)
    {
        if (FindTuner(player) is not { } tuner)
        {
            player.SendHint("<size=23><color=#ff7777>Access Tuner が必要です。</color></size>", 4f);

            return;
        }

        if (tuner.ApplyDataCell(player, CellLevel))
            Destroy();
    }

    /// <summary>
    /// 持ち物の中で一番レベルの高い診断装置を返します。手に持っていればそれを優先します。
    /// </summary>
    private static AccessTuner FindTuner(Player player)
    {
        if (Of(player.CurrentItem) is AccessTuner held) return held;

        AccessTuner best = null;

        foreach (Item item in player.Items)
        {
            if (Of(item) is AccessTuner tuner && (best is null || tuner.Level > best.Level))
                best = tuner;
        }

        return best;
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.PickingUpItem += OnAnyPickingUp;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.PickingUpItem -= OnAnyPickingUp;
            hooked = false;
        });
    }

    private static void OnAnyPickingUp(PlayerPickingUpItemEventArgs ev)
    {
        if (Of(ev.Pickup) is not DataCell cell) return;

        ev.IsAllowed = false;
        cell.Use(ev.Player);
    }
}
