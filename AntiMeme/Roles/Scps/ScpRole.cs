using AntiMeme.Hud;
using AntiMeme.Net;
using AntiMeme.Teams;
using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.PlayerEvents;
using PlayerStatsSystem;
using Sliced.API.Features;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP 役職の共通土台です。所属チーム・説明の表示秒数・終了放送の文面を持ちます。
/// イベント購読・ステータス表示・ヒュームシールドは <see cref="CustomRole"/> にあります。
/// 部屋を基準にしたスポーン座標は <see cref="SpawnPoints.InRoom"/> を使ってください。
/// </summary>
/// <remarks>
/// 旧実装は終了放送を各 SCP 役職が <c>CassieHelper.AnnounceTermination</c> で
/// 手書きしていました (14 箇所)。読ませる名前だけが役職ごとに違うので、
/// <see cref="CassieName"/> を 1 行宣言すれば <see cref="ScpTerminationAnnouncer"/> が放送します。
/// </remarks>
public abstract class ScpRole : CustomRole
{
    /// <summary>
    /// 既定の所属は SCP 陣営です。
    /// 別の陣営に属する SCP (SCP-3005 は第五教会、SCP-999 はどこにも属さない) だけ override します。
    /// </summary>
    public override CustomTeam Team => CustomTeam.Get<ScpTeam>();

    /// <summary>
    /// 説明ヒントの表示秒数です。SCP は説明が長いので既定を伸ばしてあります。
    /// </summary>
    protected override float HintDuration => 8.5f;

    /// <summary>
    /// 終了放送で C.A.S.S.I.E に読ませる名前です。数字は 1 桁ずつ空けます ("SCP 0 4 9")。
    /// null なら放送しません。
    /// </summary>
    protected internal virtual string CassieName => null;

    /// <summary>
    /// 死亡したときの終了放送です。死因は攻撃者から自動で決まります。
    /// 特別な文面が要る役職はこれを override してください。
    /// </summary>
    protected internal virtual void AnnounceTermination(PlayerDyingEventArgs ev) =>
        FacilityAnnouncer.Terminate(
            CassieName,
            $"<color={Team?.Color ?? "#ffffff"}>{Name}</color>",
            CauseOf(ev),
            clear: true);

    /// <summary>
    /// 死因です。H.I.D タレットはタレット NPC が攻撃者になるので、陣営判定より先に見ます。
    /// </summary>
    protected static TerminationCause CauseOf(PlayerDyingEventArgs ev)
    {
        if (ev.Attacker is not { IsDestroyed: false } attacker)
            return TerminationCause.Unknown();

        if (ev.DamageHandler is MicroHidDamageHandler && InternalNpcs.Is(attacker, InternalNpcKind.HidTurret))
            return TerminationCause.Hid();

        return CustomTeam.Of(attacker) is { } faction
            ? TerminationCause.ByTeam(faction, faction.CassieName)
            : TerminationCause.Unknown();
    }
}
