using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Features;
using AntiMeme.Spawning;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// 単発の仕掛けを手で起こします。検証用です。
/// </summary>
/// <remarks>
/// 旧実装は仕掛けごとにコマンドクラスを 1 つ作っていました
/// (<c>PlayInstantSurfaceBombing</c> / <c>PlayOmegaWarhead</c> / <c>ActivateGenerator</c> …)。
/// どれも「呼ぶだけ」なので、名前と呼び先の表 1 本で足ります。
/// </remarks>
public sealed class TriggerCommand : CommandBase
{
    /// <summary>手で起こせる仕掛けです。名前 → 実行する処理。</summary>
    private static readonly Dictionary<string, Func<string>> Triggers =
        new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["bombing"] = () => SurfaceBombingFunction.TryStart()
                ? "地上爆撃を開始しました。"
                : "開始できません (ラウンド中でないか、既に進行中です)。",

            ["generators"] = ActivateAllGenerators,

            ["pandra"] = () =>
            {
                EventHandlerRegistry.AutoRegistered.OfType<PandraBoxDispatch>().FirstOrDefault()?.Queue();

                return "Pandra's Box の派遣を予約しました (観戦者が 2 人揃うと来ます)。";
            },
        };

    public override Type Parent => typeof(RootCommand);

    public override string Command => "trigger";

    public override string Usage => "trigger <" + "bombing|generators|pandra" + ">";

    public override string Description => "単発の仕掛けを手で起こします。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string name))
        {
            response = $"起こせるもの: {string.Join(" / ", Triggers.Keys)}";

            return false;
        }

        if (!Triggers.TryGetValue(name, out Func<string> trigger))
        {
            response = $"{name} は知らない仕掛けです。起こせるもの: {string.Join(" / ", Triggers.Keys)}";

            return false;
        }

        response = trigger();

        return true;
    }

    private static string ActivateAllGenerators()
    {
        List<Generator> pending = Generator.List.Where(generator => !generator.Engaged).ToList();

        foreach (Generator generator in pending)
            generator.Engaged = true;

        return $"発電機 {pending.Count} 基を起動しました。";
    }
}
