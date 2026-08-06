using System;
using System.Linq;
using AntiMeme.Spawning;
using AntiMeme.Spawning.Waves;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// ウェーブをその場で湧かせるか、次のバニラウェーブを差し替えます。
/// <c>am wave now HdWave</c> / <c>am wave next GoCWave</c>。
/// </summary>
public sealed class WaveCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "wave";

    public override string Usage => "wave <now|next|clear> [ウェーブのクラス名] [mini]";

    public override string Description => "ウェーブを即時スポーンするか、次のウェーブを差し替えます。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string action))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        if (string.Equals(action, "clear", StringComparison.OrdinalIgnoreCase))
        {
            SpawnSystem.ClearNextSpawnOverrides("command");
            response = "ウェーブの上書きを取り消しました。";

            return true;
        }

        if (!TryGetArgument(1, out string typeName) || SpawnContext.Find(typeName) is not { } wave)
        {
            response = $"'{typeName}' は知らないウェーブです。指定できるのは: " +
                       $"{string.Join(", ", SpawnContext.AllWaves.Select(candidate => candidate.GetType().Name))}";

            return false;
        }

        bool mini = TryGetArgument(2, out string flag) &&
                    string.Equals(flag, "mini", StringComparison.OrdinalIgnoreCase);

        switch (action.ToLowerInvariant())
        {
            case "now":
                SpawnSystem.ForceSpawnNow(wave, mini);
                response = $"{wave.Name} を即時スポーンしました。";

                return true;

            case "next":
                SpawnSystem.ReplaceNextSpawn(wave, mini, "command");
                response = $"次のウェーブを {wave.Name} に差し替えました。";

                return true;

            default:
                response = $"'{action}' は知らない操作です。使い方: am {Usage}";

                return false;
        }
    }
}
