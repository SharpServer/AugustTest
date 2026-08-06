using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

using AntiMemeAnnouncer = AntiMeme.Hud.FacilityAnnouncer;

namespace AntiMeme.Maps.Objects;

/// <summary>
/// Femur Breaker。装置の前に立った者を捕らえ、ボタンを押すと 28 秒後に処刑します。
/// SCP-106 が居れば、その処刑をもって再収容が成立します。
/// </summary>
/// <remarks>
/// 旧実装は「監視コルーチン + 捕縛リスト + ボタンの位置照合 + 施設ハンドラからの
/// <c>SetDoor</c>/<c>SetButton</c> 注入」の 4 点セットでしたが、
/// 扉もボタンもこのスキマティックの一部なので、装置自身が全部持てば足ります。
/// </remarks>
public sealed class FemurBreaker : ObjectPrefab
{
    private const float JoinRadius = 1.6f;
    private const float WatchInterval = 0.5f;
    private const float ExecutionDelay = 28f;
    private const float DoorDrop = 2.5f;
    private const float DoorSlideSeconds = 0.65f;
    private const string DeathReason = "Femur Breaker の犠牲となった";
    private const string RecontainReason = "Femur Breaker によって再収容された";

    private readonly List<Player> captured = new List<Player>();

    private bool activated;

    /// <inheritdoc/>
    protected override string SchematicName => "FemurBreaker_Door";

    /// <summary>捕縛地点です。ここに立った者が捕らえられます。</summary>
    private static Vector3? JoinPoint =>
        MapPoints.TryGet("FemurBreaker_JoinPoint", out Vector3 point) ? point : null;

    /// <summary>捕縛後に移送される、装置の中の位置です。</summary>
    private static Vector3? ChamberPoint =>
        MapPoints.TryGet("FemurBreaker_CapybaraPoint", out Vector3 point) ? point : null;

    /// <inheritdoc/>
    protected override void OnSetup()
    {
        if (GetInteractable("Button") is { } button)
            button.Interacted += ev => Activate(ev.Player);

        Loop(WatchInterval, Watch);
    }

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        captured.Clear();
        activated = false;
    }

    /// <summary>
    /// 捕縛地点に立った最初の非 SCP を捕らえます。1 度捕らえたら監視は終わりです。
    /// </summary>
    private void Watch()
    {
        if (activated || captured.Count > 0 || JoinPoint is not { } join || ChamberPoint is not { } chamber) return;

        Player target = Player.ReadyList.FirstOrDefault(player =>
            player is { IsDestroyed: false, IsAlive: true } &&
            !player.IsSCP &&
            (player.Position - join).sqrMagnitude <= JoinRadius * JoinRadius);

        if (target is null) return;

        target.IsDisarmed = true;
        target.Position = chamber;
        captured.Add(target);

        LowerDoor();
    }

    /// <summary>扉を落として装置を閉じます。</summary>
    private void LowerDoor() => MoveTo(Position, Position + Vector3.down * DoorDrop, DoorSlideSeconds);

    private void Activate(Player player)
    {
        if (captured.Count == 0 || activated)
        {
            player?.SendHint("<size=24>準備が完了していないか、既に実行されています。</size>", 3f);

            return;
        }

        activated = true;

        MapAudio.Play("FemurBreaker.ogg", "FemurBreaker", Position, maxDistance: 60f);

        Delay(ExecutionDelay, Execute);
    }

    private void Execute()
    {
        foreach (Player victim in captured.Where(victim => victim is { IsDestroyed: false, IsAlive: true }))
            victim.Kill(DeathReason);

        List<Player> scp106 = Player.ReadyList
            .Where(player => player is { IsDestroyed: false, IsAlive: true } && IsScp106(player))
            .ToList();

        foreach (Player player in scp106)
            player.Kill(RecontainReason);

        Announce(scp106.Count > 0);

        captured.Clear();
    }

    private static void Announce(bool recontained)
    {
        if (recontained)
        {
            AntiMemeAnnouncer.Say(
                "SCP 1 0 6 recontained successfully by femur breaker",
                "<color=red>SCP-106</color>のFEMUR BREAKERによる再収容に成功しました。");

            return;
        }

        AntiMemeAnnouncer.Say(
            "Femur Breaker Process Successfully Completed . but no effect for containment breach .",
            "FEMUR BREAKERプロセスが正常に完了しましたが、収容違反への影響が確認されませんでした。");
    }

    /// <summary>
    /// SCP-106 かどうか。カスタム役職を被せていない素の 106 も対象です。
    /// </summary>
    private static bool IsScp106(Player player) =>
        CustomRole.Of(player) is null && player.Role == RoleTypeId.Scp106;
}
