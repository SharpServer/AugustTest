using System.Collections.Generic;
using System.Linq;
using AntiMeme.Hud;
using AntiMeme.Roles.Scps;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Maps.Features;

/// <summary>
/// アンチミームプロトコル。反ミーム性の存在 (SCP-3005 / SCP-3125) を無力化します。
/// 一度動かすと施設全体に通達が流れ、以降は再開・停止を切り替えるだけになります。
/// </summary>
public sealed class AntiMemeProtocolFunction : FacilityControlRoomFunction
{
    private const float FirstActivationHealth = 10000f;
    private const string FifthistColor = "#ff00fa";

    /// <summary>いま作動しているか。SCP-3005 側の演出が参照します。</summary>
    public static bool IsActive { get; private set; }

    /// <summary>このラウンドで一度でも作動したか。初回だけ体力を積み増すために見ます。</summary>
    public static bool HasActivatedInPast { get; private set; }

    /// <inheritdoc/>
    public override string DisplayName => "アンチミームプロトコル";

    /// <inheritdoc/>
    public override string Description => "SCP-3005 / SCP-3125 に対する反ミーム性無力化処理を開始または停止する。";

    /// <inheritdoc/>
    public override int Order => 0;

    /// <inheritdoc/>
    public override DoorPermissionFlags RequiredPermissions => DoorPermissionFlags.ContainmentLevelThree;

    /// <inheritdoc/>
    public override void ResetState()
    {
        IsActive = false;
        HasActivatedInPast = false;
    }

    /// <inheritdoc/>
    public override FacilityControlRoomFunctionResult Execute(FacilityControlRoomFunctionContext context) =>
        IsActive ? Stop() : Start(context.Player);

    private static FacilityControlRoomFunctionResult Start(Player player)
    {
        // 第五教会が自分たちの側を守る使い方をさせない。
        if (CustomTeam.Of(player) is FifthistTeam)
            return Failure("<color=#ff5555>第五教会はアンチミームプロトコルを開始できません。</color>");

        List<Player> targets = Targets().ToList();

        if (targets.Count == 0)
            return Failure("<color=#ff5555>対象が見つかりませんでした。</color>");

        foreach (Player target in targets)
        {
            // 初回だけは「無力化に耐えた」ぶんの体力を与えて、即死しないようにする。
            if (!HasActivatedInPast)
                target.Health = FirstActivationHealth;

            target.EnableEffect<Poisoned>(255);
            target.EnableEffect<Decontaminating>(255);
        }

        Announce();
        IsActive = true;

        return Success("アンチミームプロトコルを開始しました。");
    }

    private static FacilityControlRoomFunctionResult Stop()
    {
        foreach (Player target in Targets())
        {
            target.DisableEffect<Poisoned>();
            target.DisableEffect<Decontaminating>();
        }

        FacilityAnnouncer.Say(
            "$pitch_.85 Anti- $pitch_1 Me mu Protocol Stopped .",
            $"<color={FifthistColor}>アンチミームプロトコル</color>が停止されました。",
            noise: false);

        IsActive = false;

        return Success("アンチミームプロトコルを停止しました。");
    }

    private static void Announce()
    {
        if (HasActivatedInPast)
        {
            FacilityAnnouncer.Say(
                "$pitch_.85 Anti- $pitch_1 Me mu Protocol Resumed .",
                $"<color={FifthistColor}>アンチミームプロトコル</color>が再開されました。",
                noise: false);

            return;
        }

        FacilityAnnouncer.Say(
            "By order of Facility Manager Control Room , $pitch_.85 Anti- $pitch_1 Me mu Protocol Activated .",
            $"<color=#ff0087>施設管理者制御室</color>からの命令により、" +
            $"<color={FifthistColor}>アンチミームプロトコル</color>が有効化されました。" +
            "エージェントにより反ミーム性物体の非活性化が開始されます。");

        HasActivatedInPast = true;
    }

    /// <summary>無力化の対象です。反ミーム性を持つ 2 体だけが該当します。</summary>
    private static IEnumerable<Player> Targets() =>
        Player.ReadyList.Where(player =>
            player is { IsDestroyed: false, IsAlive: true } && CustomRole.Of(player) is Scp3005 or Scp3125);
}
