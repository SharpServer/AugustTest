using System;
using AntiMeme.GameModes;
using Sliced.API.Features;
using AntiMeme.GameModes.Modes;

namespace AntiMeme.Commands;

/// <summary>
/// ゲームモードの起動・予約・停止です。
/// <c>am mode run CandyWarriorsAttack</c> で即起動、
/// <c>am mode queue CandyWarriorsAttack</c> で次ラウンドに予約します。
/// </summary>
public sealed class ModeCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "mode";

    public override string Usage => "mode <run|queue|stop|clear|roll> [モードクラス名]";

    public override string Description => "ゲームモードを起動・予約・停止します。引数なしで現在のモードを表示します。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string action))
        {
            response = GameMode.Current is { } running
                ? $"現在のモード: {running.Name} ({running.GetType().Name})"
                : "現在動いているモードはありません。";

            return true;
        }

        switch (action.ToLowerInvariant())
        {
            case "run":
                return Run(out response);

            case "queue":
                return Queue(out response);

            case "stop":
                GameMode.StopCurrent();
                response = "現在のモードを停止しました。";

                return true;

            case "clear":
                GameModeSelection.ClearQueue();
                response = "予約を空にしました。";

                return true;

            case "roll":
                if (GameModeSelection.Next() is not { } rolled)
                {
                    response = "起動できるモードがありません (最小人数を満たしていない可能性があります)。";

                    return false;
                }

                rolled.Start();
                response = $"{rolled.Name} を抽選で起動しました。";

                return true;

            default:
                response = $"'{action}' は知らない操作です。使い方: am {Usage}";

                return false;
        }
    }

    private bool Run(out string response)
    {
        if (!TryResolve(out GameMode mode, out response)) return false;

        if (!mode.Start())
        {
            response = $"{mode.Name} の起動に失敗しました。";

            return false;
        }

        response = $"{mode.Name} を起動しました。";

        return true;
    }

    private bool Queue(out string response)
    {
        if (!TryGetArgument(1, out string modeName))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        if (!TypeParser.TryParse<GameMode>(modeName, out Type modeType))
        {
            response = $"'{modeName}' というモードは見つかりませんでした。 (am list modes で一覧)";

            return false;
        }

        GameModeSelection.Enqueue(modeType);
        response = $"{modeType.Name} を予約しました (予約 {GameModeSelection.Pending.Count} 件)。";

        return true;
    }

    private bool TryResolve(out GameMode mode, out string response)
    {
        mode = null;

        if (!TryGetArgument(1, out string modeName))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        if (!TypeParser.TryCreate(modeName, out mode))
        {
            response = $"'{modeName}' というモードは見つかりませんでした。 (am list modes で一覧)";

            return false;
        }

        response = string.Empty;

        return true;
    }
}
