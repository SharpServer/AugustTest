using System;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Commands;

/// <summary>
/// アビリティを付与するか、その場で発動します。
/// <c>am ability TeleportRandomAbility</c> で付与、
/// <c>am ability TeleportRandomAbility @me use</c> で発動します。
/// </summary>
public sealed class AbilityCommand : CommandBase
{
    public override Type Parent => typeof(RootCommand);

    public override string Command => "ability";

    public override string Usage => "ability <アビリティクラス名> [対象] [use]";

    public override string Description => "アビリティを付与します。末尾に use を付けるとその場で発動します。";

    protected override bool OnExecute(out string response)
    {
        if (!TryGetArgument(0, out string abilityName))
        {
            response = $"使い方: am {Usage}";

            return false;
        }

        if (!TryGetPlayer(1, out Player target))
        {
            response = "対象のプレイヤーが見つかりませんでした。サーバーコンソールからは対象を明示してください。";

            return false;
        }

        if (!TypeParser.TryParse<AbilityBase>(abilityName, out Type abilityType))
        {
            response = $"'{abilityName}' というアビリティは見つかりませんでした。 (am list abilities で一覧)";

            return false;
        }

        if (AbilityBase.Give(abilityType, target) is not { } ability)
        {
            response = $"{abilityType.Name} を付与できませんでした。";

            return false;
        }

        if (!TryGetArgument(2, out string mode) || !string.Equals(mode, "use", StringComparison.OrdinalIgnoreCase))
        {
            response = $"{target.Nickname} に {ability.Name} を付与しました。";

            return true;
        }

        if (!ability.TryUse(out string failureReason))
        {
            response = $"{ability.Name} を発動できませんでした: {failureReason}";

            return false;
        }

        response = $"{target.Nickname} の {ability.Name} を発動しました。";

        return true;
    }
}
