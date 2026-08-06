using Sliced.API.Structs;
using PlayerRoles;
using System.Linq;
using AntiMeme.Maps.Features;
using AntiMeme.Roles.Scientist;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// 第五教会です。SCP-3005 もここに属します。
/// </summary>
public sealed class FifthistTeam : CustomTeam
{
    public override string Name => "第五教会";

    /// <inheritdoc/>
    public override string HudName => "<color=#ff00fa>The Fifthists</color>";

    /// <inheritdoc/>
    public override string Objective => "第五を広め、施設を占領せよ。";

    // CASSIE に「5 5 5」と読ませるためのピッチ指定。
    public override string CassieName => "$pitch_1.05 5 5 5 $pitch_1 Forces";

    public override string Color => "#ff00fa";

    public override VictoryCondition Victory => VictoryCondition.LastStanding(priority: 10);

    public override bool UsesVanillaEnding => false;

    /// <inheritdoc/>
    public override bool ShowsRoster => true;

    /// <summary>
    /// 標的 (Marion Wheeler) の居場所と、反ミームプロトコルの状況を出します。
    /// </summary>
    public override string RosterFooter(Player viewer)
    {
        string target = TargetLine(viewer);
        string protocol = ProtocolLine();

        return string.Join("\n", new[] { target, protocol }.Where(line => line.Length > 0));
    }

    // バニラ役職だけの第五教会員は居ない。全員カスタム役職で名乗る。
    /// <summary>SCP-1509 で蘇生した者はこの陣営の一員として立ちます。</summary>
    public override SpawnSetRoleDefinition? Resurrection => SpawnSetRoleDefinition.Custom<AntiMeme.Roles.Fifthist.FifthistConvert>();

    protected override bool IncludesVanilla(Player player) => false;

    private string TargetLine(Player viewer)
    {
        Player marion = Player.ReadyList.FirstOrDefault(player =>
            player is { IsDestroyed: false, IsAlive: true } && CustomRole.Of(player) is MarionWheeler);

        if (marion is null) return string.Empty;

        string line = $"<color={Color}>第五目標:</color> Marion Wheeler / {marion.Zone}";

        if (ReferenceEquals(marion, viewer)) return line;

        return $"{line} / {(int)Vector3.Distance(viewer.Position, marion.Position)}m";
    }

    private static string ProtocolLine()
    {
        if (AntiMemeProtocolFunction.IsActive)
            return "<color=red>反ミームプロトコル: 起動中</color>";

        return AntiMemeProtocolFunction.HasActivatedInPast
            ? "<color=orange>反ミームプロトコル: 起動履歴あり</color>"
            : string.Empty;
    }
}
