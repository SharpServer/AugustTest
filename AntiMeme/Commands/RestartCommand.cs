using System;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// ラウンドを再開します。<c>fast</c> を付けるとロビーを挟まずに再開します。
/// </summary>
public sealed class RestartCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "restart";

    public override string Usage => "restart [fast]";

    public override string Description => "ラウンドを再開します。";

    protected override bool OnExecute(out string response)
    {
        bool fast = TryGetArgument(0, out string flag) &&
                    string.Equals(flag, "fast", StringComparison.OrdinalIgnoreCase);

        Round.Restart(fast);

        response = fast ? "ラウンドを即時再開しました。" : "ラウンドを再開しました。";

        return true;
    }
}
