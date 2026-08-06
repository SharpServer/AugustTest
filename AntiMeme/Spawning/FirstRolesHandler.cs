using System;
using System.Linq;
using AntiMeme.Maps.Features;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Spawning;

/// <summary>
/// ラウンド開始時の役職割り当てを、バニラから奪って自前で行います。
/// <see cref="EventHandlerBase"/> を継承しているだけで Sliced が購読します。
///
/// <para>
/// バニラの <c>PlayerRoles.RoleAssign.RoleAssigner</c> を止めるために、
/// 理由が <see cref="RoleChangeReason.RoundStart"/> の役職変更を常に拒否します。
/// 開始時の割り当ては完全にこちらが持つので、条件やフラグは要りません。
/// 遅れて参加したプレイヤーは <see cref="RoleChangeReason.LateJoin"/> なので影響しません。
/// </para>
/// <para>
/// SCP を先に決めてから、残り全員を人間側に配ります。
/// 割り当て中のラウンドロックは <see cref="SpawnSet.Spawn"/> が自分で面倒を見ます。
/// </para>
/// </summary>
public sealed class FirstRolesHandler : EventHandlerBase
{
    public override void OnPlayerChangingRole(PlayerChangingRoleEventArgs ev)
    {
        if (ev.ChangeReason is RoleChangeReason.RoundStart)
            ev.IsAllowed = false;
    }

    public override void OnServerRoundStarted()
    {
        ReleaseLobbyPlayers();

        // SCP 側が落ちても人間側は必ず配る。
        // ここで例外を通すと LabApi がハンドラごと打ち切るので、
        // 「SCP だけ居て他は全員観戦」のままラウンドが終わります。
        Assign(new FirstRolesScps());
        Assign(new FirstRolesHumans());

        Report();
    }

    private static void Assign(SpawnSet set)
    {
        try
        {
            set.Spawn();
        }
        catch (Exception exception)
        {
            Logger.Error($"[AntiMeme] {set.Name} の割り当てに失敗しました: {exception}");
        }
    }

    /// <summary>
    /// 誰にも役職が渡らなかったときだけ知らせます。
    /// 黙って全員観戦のままラウンドが終わるのが一番たちが悪いので、原因の入口を残します。
    /// </summary>
    private static void Report()
    {
        int assigned = Player.ReadyList.Count(player =>
            player is { IsDestroyed: false, IsPlayer: true } &&
            player.Role is not (RoleTypeId.None or RoleTypeId.Spectator or RoleTypeId.Overwatch or RoleTypeId.Tutorial));

        if (assigned > 0) return;

        Logger.Error(
            "[AntiMeme] ラウンド開始時に誰にも役職が渡りませんでした。" +
            $"対象人数: {Player.ReadyList.Count(player => player.IsPlayer)}");
    }

    /// <summary>
    /// ロビー演出はプレイヤーを Tutorial にして待たせます。
    /// <see cref="SpawnSet.TargetPlayers"/> は未割り当て (None / Spectator) しか拾わないので、
    /// 割り当ての前に観戦へ戻して同じ土俵に乗せます。
    /// </summary>
    /// <remarks>
    /// <b>ロビー側が覚えている一覧には頼りません。</b>
    /// <see cref="Lobby"/> も <c>RoundStarted</c> で後始末をして
    /// <see cref="Lobby.Waiting"/> を空にするため、どちらが先に呼ばれるかで
    /// 解放できたりできなかったりします (解放し損ねると全員 Tutorial のままになり、
    /// 誰にも役職が渡りません)。今 Tutorial に居る人を直接見ます。
    /// </remarks>
    private static void ReleaseLobbyPlayers()
    {
        foreach (Player player in Player.ReadyList.ToArray())
        {
            if (player is { IsDestroyed: false } && player.Role is RoleTypeId.Tutorial)
                player.SetRole(RoleTypeId.Spectator, RoleChangeReason.None, RoleSpawnFlags.None);
        }

        Lobby.Waiting.Clear();
    }
}
