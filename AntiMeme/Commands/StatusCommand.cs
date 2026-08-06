using System;
using System.Linq;
using System.Text;
using AntiMeme.GameModes;
using AntiMeme.Maps;
using AntiMeme.Spawning;
using AntiMeme.Spawning.Waves;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// 実行時の状態を 1 画面にまとめます。
/// 自動登録が効いているか (常駐ハンドラ数が 0 でないか) の確認にも使います。
/// </summary>
public sealed class StatusCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "status";

    public override string Description => "ラウンド・ゲームモード・ウェーブ・登録済みハンドラの状態を表示します。";

    protected override bool OnExecute(out string response)
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("<b>AntiMeme Status</b>");
        builder.AppendLine($"  ラウンド      : {(Round.IsRoundInProgress ? "進行中" : "非進行")} / 経過 {Round.Duration:hh\\:mm\\:ss}");
        builder.AppendLine($"  プレイヤー    : {Player.ReadyList.Count()} 人");

        builder.AppendLine($"  ゲームモード  : {(GameMode.Current is { } mode ? $"{mode.Name} ({mode.GetType().Name})" : "なし")}");
        builder.AppendLine($"  予約          : {Describe(GameModeSelection.Pending.Select(type => type.Name))}");
        builder.AppendLine($"  直近          : {Describe(GameModeSelection.Recent.Select(type => type.Name))}");

        builder.AppendLine($"  スポーン文脈  : {SpawnContext.Active.Name}");
        builder.AppendLine($"  ウェーブ上書き: {SpawnSystem.PendingOverrideCount} 件" +
                           (SpawnSystem.PendingOverrideWave is { } pending ? $" (次 {pending.Name})" : string.Empty));
        builder.AppendLine($"  初回ウェーブ  : {(FirstWaveScheduler.HasSpawned ? "済" : $"{FirstWaveScheduler.ElapsedSeconds:F0} / {FirstWaveScheduler.WaitSeconds:F0} 秒")}");

        builder.AppendLine($"  カスタム役職  : {CustomRole.Active.Count} 人");
        builder.AppendLine($"  カスタムアイテム: {CustomItem.Tracked.Count} 個");
        builder.AppendLine($"  ObjectPrefab  : {ObjectPrefab.All.Count} 個");
        builder.AppendLine($"  ハンドラ      : 自動登録 {EventHandlerRegistry.AutoRegistered.Count} 件 / 有効 {EventHandlerBase.Active.Count} 件");

        response = builder.ToString().TrimEnd();

        return true;
    }

    private static string Describe(System.Collections.Generic.IEnumerable<string> names)
    {
        string joined = string.Join(", ", names);

        return joined.Length == 0 ? "なし" : joined;
    }
}
