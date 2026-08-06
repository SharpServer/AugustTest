using System;
using AntiMeme.Hud;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// 自分のデバッグ HUD を切り替えます。マップ作成時に座標とドアのローカル値を読むためのものです。
/// </summary>
public sealed class DebugCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "debug";

    public override string Description => "自分のデバッグ HUD を切り替えます。";

    protected override bool OnExecute(out string response)
    {
        if (Sender is null)
        {
            response = "この操作はゲーム内から実行してください。";

            return false;
        }

        response = DebugMode.Toggle(Sender)
            ? "デバッグ HUD を表示します。"
            : "デバッグ HUD を隠しました。";

        return true;
    }
}
