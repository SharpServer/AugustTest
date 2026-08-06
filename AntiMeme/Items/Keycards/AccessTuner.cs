using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// 施設の扉をハックする診断装置です。カードとしての権限は持たず、
/// 扉を開けるたびに <see cref="Points"/> を消費します。
///
/// <para>
/// レベルは <see cref="Items.Keycards.DataCell"/> を当てると上がります。
/// レベルが変わっても<b>アイテムは同じまま</b>なので、
/// 見た目と表示名は <see cref="CustomKeycard.Refresh"/> で焼き直すだけで済みます。
/// </para>
/// </summary>
/// <remarks>
/// 旧実装 (539 行) はレベルごとに別のカスタムアイテムを立て、強化のたびに
/// <c>AccessTunerService</c> をシリアル辞書間で移し替えて
/// <c>SerialTracker.ForceRegister</c> で登録先の型を差し替えていました。
/// アイテム 1 個 = 1 インスタンスなので、レベルはただのフィールドで足ります。
/// </remarks>
public abstract class AccessTuner : CustomKeycard
{
    /// <summary>ハックできる最大のレベルです。</summary>
    public const int MaxLevel = 3;

    /// <summary>特殊扉 1 回分のポイントです。</summary>
    public const int SpecialAccessCost = 20;

    private static bool hooked;

    private int level;
    private int points;
    private int uses;
    private string lastResult = "未実行";

    protected AccessTuner()
    {
        level = InitialLevel;
        Hook();
    }

    /// <summary>
    /// 現在のレベルです。0 は故障品を表します。
    /// </summary>
    public int Level => level;

    /// <summary>
    /// 残りポイントです。
    /// </summary>
    public int Points => points;

    /// <summary>
    /// 現在のレベルで貯められるポイントの上限です。
    /// </summary>
    public int MaxPoints => MaxPointsOf(level);

    /// <summary>
    /// 生成時のレベルです。0 は故障品。
    /// </summary>
    protected abstract int InitialLevel { get; }

    /// <inheritdoc/>
    /// <remarks>レベルが上がると見た目も変わります。</remarks>
    protected override string PickupModel => level switch
    {
        1 => "Alienisolation_AccessTuner_Lv1",
        2 => "Alienisolation_AccessTuner_Lv2",
        3 => "Alienisolation_AccessTuner_Lv3",
        _ => "Alienisolation_AccessTuner_Broken",
    };

    /// <inheritdoc/>
    public override string Name => level > 0 ? $"Access Tuner Level-{level}" : "Broken Access Tuner";

    /// <inheritdoc/>
    public override string Description => level switch
    {
        1 => "施設のメンテナンスに使用される整備用の診断装置。\n" +
             "Lv1のデータセルによって低い権限の扉や施設に対してハッキングできる。",
        2 => "施設のメンテナンスに使用される整備用の診断装置。\n" +
             "Lv2のデータセルによってある程度の権限の扉や施設に対してハッキングできる。",
        3 => "施設のメンテナンスに使用される整備用の診断装置。\n" +
             "Lv3のデータセルによって高い権限の扉やほぼ全ての施設に対してハッキングできる。",
        _ => "壊れた診断装置。データセルを当てれば直るかもしれない。",
    };

    /// <summary>
    /// カードそのものは何の権限も持ちません。扉はハックで開けます。
    /// </summary>
    protected override KeycardLevels Levels => default;

    /// <inheritdoc/>
    protected override string Label => level > 0 ? $"TUNER Lv.{level}" : "TUNER --";

    /// <inheritdoc/>
    protected override Color32 LabelColor => new Color32(20, 24, 28, 255);

    /// <inheritdoc/>
    protected override Color32 Tint => level switch
    {
        1 => new Color32(235, 235, 235, 255),
        2 => new Color32(255, 115, 0, 255),
        3 => new Color32(230, 30, 30, 255),
        _ => new Color32(110, 110, 110, 255),
    };

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(18, 18, 18, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Maintenance";

    /// <summary>
    /// ポイントを足します。上限を超えた分は捨てられます。
    /// スネークなどのミニゲーム側から充填するための入り口です。
    /// </summary>
    public void AddPoints(int amount)
    {
        points = Mathf.Clamp(points + amount, 0, MaxPoints);
        Report(Owner);
    }

    /// <summary>
    /// データセルを当てます。消費された場合に true を返します。
    /// </summary>
    public bool ApplyDataCell(Player player, int cellLevel)
    {
        if (cellLevel < level)
        {
            player.SendHint("<size=23><color=#ff7777>Access Tuner より低い Lv の Data Cell は使用できません。</color></size>", 4f);

            return false;
        }

        if (cellLevel > level)
        {
            level = cellLevel;
            lastResult = $"Data Cell同期（Lv.{level}へ強化）";
            Refresh();
            RefreshPickupModel();
        }
        else
        {
            lastResult = "Data Cell同期（ポイント最大）";
        }

        points = MaxPoints;
        Report(player);

        return true;
    }

    /// <summary>
    /// マップ側の特殊扉を開けるためにポイントを使います。
    /// Lv.3 かつ <see cref="SpecialAccessCost"/> ポイント以上のときだけ通ります。
    /// </summary>
    public bool TryConsumeSpecialAccess(Player player)
    {
        if (level < MaxLevel)
        {
            lastResult = "失敗（特殊扉権限不足）";
            Report(player);

            return false;
        }

        if (points < SpecialAccessCost)
        {
            lastResult = $"失敗（特殊扉には{SpecialAccessCost}ポイント必要）";
            Report(player);

            return false;
        }

        points -= SpecialAccessCost;
        uses++;
        lastResult = "成功（特殊扉）";
        Report(player);

        return true;
    }

    /// <summary>
    /// 扉をハックします。開けられたら true。
    /// </summary>
    private bool TryHack(Player player, KeycardLevels required)
    {
        if (required.Containment > level || required.Armory > level || required.Admin > level)
        {
            lastResult = $"失敗（権限不足: Lv.{required.HighestLevelValue}必要）";
            Report(player);

            return false;
        }

        int cost = CostOf(required.HighestLevelValue);

        if (points < cost)
        {
            lastResult = $"失敗（ポイント不足: {cost}必要）";
            Report(player);

            return false;
        }

        points -= cost;
        uses++;
        lastResult = $"成功（Lv.{required.HighestLevelValue}扉）";
        Report(player);

        return true;
    }

    /// <summary>
    /// 現在の状態を持ち主に見せます。
    /// </summary>
    private void Report(Player player)
    {
        if (player is not { IsDestroyed: false }) return;

        string special = level >= MaxLevel && points >= SpecialAccessCost
            ? "<color=#88ff88>可能</color>"
            : "<color=#ff7777>不可</color>";

        player.SendHint(
            $"<size=23><color=#66ddff><b>ACCESS TUNER Lv.{level}</b></color>\n" +
            $"ポイント: <color=#ffee77>{points}/{MaxPoints}</color>　使用回数: {uses}\n" +
            $"権限: C{level} / A{level} / AD{level}　特殊扉: {special}\n" +
            $"直近の結果: {lastResult}</size>",
            4f);
    }

    private static int MaxPointsOf(int level) => level switch
    {
        1 => 25,
        2 => 50,
        3 => 100,
        _ => 0,
    };

    private static int CostOf(int level) => level switch
    {
        1 => 5,
        2 => 10,
        3 => 20,
        _ => 0,
    };

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.InteractingDoor += OnAnyInteractingDoor;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.InteractingDoor -= OnAnyInteractingDoor;
            hooked = false;
        });
    }

    private static void OnAnyInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        // 既に開けられる扉と、ロックダウン中の扉には手を出さない。
        if (ev.CanOpen || ev.Door.IsLocked) return;

        if (Of(ev.Player.CurrentItem) is not AccessTuner tuner) return;

        KeycardLevels required = new KeycardLevels(ev.Door.Permissions);

        if (required.HighestLevelValue == 0) return;

        ev.CanOpen = tuner.TryHack(ev.Player, required);
    }
}
