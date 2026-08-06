using System.Globalization;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Teams.Factions;

/// <summary>
/// 季節イベントの戦士達です。呼称は <see cref="Config.Season"/> で変わります。
/// </summary>
public sealed class WarriorsTeam : CustomTeam
{
    public override string Name => "戦士達";

    /// <inheritdoc/>
    public override string HudName => "<b><color=#ffffff>WARRIER's DIVISION</color></b>";

    /// <inheritdoc/>
    public override string Objective => "全施設に季節の正義を執行しろ";

    public override string CassieName => "Warriors";

    public override string Color => "#ffffff";

    public override bool UsesVanillaEnding => false;

    /// <inheritdoc/>
    public override bool ShowsRoster => true;

    /// <summary>
    /// 所属部隊名と、進行中なら Alpha Warhead の残り時間を出します。
    /// </summary>
    public override string RosterFooter(Player viewer)
    {
        string command = $"<color={Color}>COMMAND:</color> {Division}";

        if (!Warhead.IsDetonationInProgress) return command;

        string remaining = Warhead.DetonationTime.ToString("F0", CultureInfo.InvariantCulture);

        return $"{command}\n<color=red>ALPHA WARHEAD:</color> T-{remaining}s";
    }

    protected override bool IncludesVanilla(Player player) => false;

    private static string Division => AntiMemePlugin.Settings.Season switch
    {
        Season.Christmas => "SNOW DIVISION",
        Season.April => "CANDY DIVISION",
        Season.Halloween => "HALLOWEEN CANDY DIVISION",
        _ => "DIVISION COMMAND",
    };
}
