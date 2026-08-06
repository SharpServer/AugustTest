using Sliced.API.Structs;
using PlayerRoles;
using System.Collections.Generic;
using AntiMeme.Maps;
using LabApi.Features.Wrappers;
using UnityEngine;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// SCP です。
///
/// <para>
/// 旧実装の勝利条件は「SCP チームであり、かつ SCP-3005 でも SCP-999 でもない」という
/// 除外つきの述語でした。役職 ID → チームを表で引いていたので、例外を述語側に書くしかなかったためです。
/// 新実装では役職が自分でチームを名乗るので、SCP-3005 は
/// <see cref="FifthistTeam"/> を、SCP-999 は null (どの陣営でもない) を返すだけで済み、
/// <b>除外ロジックそのものが要らなくなりました。</b>
/// </para>
/// </summary>
public sealed class ScpTeam : CustomTeam
{
    public override string Name => "SCP";

    /// <inheritdoc/>
    public override string HudName => "<color=#c50000>The SCPs</color>";

    /// <inheritdoc/>
    public override string Objective => "己の本能・復讐心と利益の為に動け";

    public override string CassieName => "SCP";

    public override string Color => "#c50000";

    public override VictoryCondition Victory => VictoryCondition.LastStanding(priority: 40);

    /// <inheritdoc/>
    public override bool ShowsRoster => true;

    /// <summary>
    /// 発電機の状況です。SCP 側は起動を止めるのが仕事なので、進行度を常に見せます。
    /// </summary>
    public override string RosterFooter(Player viewer)
    {
        List<string> lines = ["発電機の状態："];

        foreach (Generator generator in Generator.List)
        {
            string room = RoomNames.Of(generator.Room, "不明");

            lines.Add($"<color={StatusColor(generator)}><b>{room}: </b>{StatusText(generator)}</color>");
        }

        return string.Join("\n", lines);
    }

    /// <summary>SCP-1509 で蘇生した者はこの陣営の一員として立ちます。</summary>
    public override SpawnSetRoleDefinition? Resurrection => SpawnSetRoleDefinition.Custom<AntiMeme.Roles.Scps.Zombified>();

    protected override bool IncludesVanilla(Player player) => player.IsSCP;

    /// <summary>起動までの進み具合。0 で未着手、1 で起動済み。</summary>
    private static float Progress(Generator generator) =>
        generator.TotalActivationTime > 0f
            ? Mathf.Clamp01(1f - generator.RemainingTime / generator.TotalActivationTime)
            : 0f;

    private static string StatusColor(Generator generator)
    {
        if (generator.Engaged) return "red";

        float progress = Progress(generator);

        if (progress <= 0f) return "white";

        return progress < 0.5f ? "yellow" : progress < 0.8f ? "orange" : "red";
    }

    private static string StatusText(Generator generator)
    {
        if (generator.Engaged) return "起動済み";

        float progress = Progress(generator);

        return progress <= 0f
            ? "未起動"
            : $"進行度: {progress:P0} (起動まで{generator.RemainingTime}秒)";
    }
}
