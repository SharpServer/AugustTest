using System.Collections.Generic;
using AntiMeme.Abilities;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-106。自分に大ダメージを与えてきた相手を「追跡対象」として捕捉します。
///
/// 追跡状態は 1 つしか持てないので、素のフィールド 2 本で足ります。
/// 旧実装は <c>Dictionary&lt;ownerId, HuntState&gt;</c> と
/// <c>Dictionary&lt;ownerId, Dictionary&lt;attackerId, float&gt;&gt;</c> を静的に持ち、
/// 役職変更・死亡・切断の 3 経路それぞれから手作業で掃除していました。
/// ここでは蓄積ダメージだけがインスタンス辞書で、後始末は <see cref="CustomRole.Scope"/> が引き受けます。
/// </summary>
public class Scp106 : ScpRole
{
    private const float DamageThreshold = 500f;
    private const float HuntDuration = 100f;
    private const float EscapeDistance = 100f;
    private const float TrackInterval = 0.5f;

    private readonly Dictionary<Player, float> damageTaken = [];

    private Player target;
    private float huntEndTime;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 1 0 6";

    public override string Name => "SCP-106";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-106</color>";

    /// <inheritdoc/>
    public override string Objective => "自身の欲求に従い、財団職員共を弄べ！";

    public override string Description =>
        "若者の叫び声大好き爺。いっぱいPDに送り込もう！\n" +
        "大きなダメージを与えた人間を追跡し、対象を執拗に追い詰めろ！";

    public override RoleTypeId BaseRole => RoleTypeId.Scp106;

    public override string CustomInfo => "SCP-106";

    protected override void OnSpawned()
    {
        AbilityBase.Give<CreateSinkholeAbility>(Player);

        Hook(
            () => PlayerEvents.Hurt += OnHurt,
            () => PlayerEvents.Hurt -= OnHurt);

        Scope.RunLoop(TrackInterval, _ => Track());
        Scope.OnDispose(_ => EndHunt("追跡を終了しました。", notify: false));
    }

    /// <summary>
    /// 追跡中の相手です。追跡していなければ null。
    /// </summary>
    public Player HuntTarget => target;

    private void OnHurt(PlayerHurtEventArgs ev)
    {
        if (!IsMine(ev.Player) || target is not null) return;
        if (ev.Attacker is not { IsAlive: true } attacker || attacker.IsSCP) return;
        if (ev.DamageHandler is not StandardDamageHandler damage) return;

        float total = damageTaken.TryGetValue(attacker, out float previous)
            ? previous + damage.TotalDamageDealt
            : damage.TotalDamageDealt;

        damageTaken[attacker] = total;

        if (total >= DamageThreshold)
            StartHunt(attacker);
    }

    private void StartHunt(Player hunted)
    {
        target = hunted;
        huntEndTime = Time.time + HuntDuration;
        damageTaken.Clear();

        hunted.EnableEffect<AnomalousTarget>(1, HuntDuration);
        hunted.EnableEffect<Traumatized>(1, HuntDuration);
        Player.EnableEffect<MovementBoost>(20, HuntDuration);
        Player.EnableEffect<Invigorated>(1, HuntDuration);
        AbilityBase.Give<Scp106TargetTeleportAbility>(Player);

        Player.SendHint(
            $"<color=#c50000><b>{hunted.Nickname}</b>を追跡対象に指定しました。</color>",
            5f);
        hunted.SendHint(
            "<color=#c50000><b>SCP-106に追跡されています。</b></color>\n" +
            $"距離を{EscapeDistance:F0}m以上離すことで追跡を解除できます。",
            5f);
    }

    private void Track()
    {
        if (target is null) return;

        if (target is not { IsDestroyed: false, IsAlive: true })
        {
            EndHunt("対象を見失いました。");

            return;
        }

        float distance = Vector3.Distance(Player.Position, target.Position);

        if (distance >= EscapeDistance)
        {
            EndHunt("対象が追跡圏外まで離れました。");

            return;
        }

        if (Time.time >= huntEndTime)
        {
            EndHunt("追跡可能時間が終了しました。");

            return;
        }

        ShowStatus(
            $"Target: {target.Nickname}\nDistance: {distance:F1}m / Time: {huntEndTime - Time.time:F0}s",
            TrackInterval + 0.2f);
    }

    private void EndHunt(string reason, bool notify = true)
    {
        if (target is null) return;

        Player hunted = target;
        target = null;

        Player.DisableEffect<MovementBoost>();
        Player.DisableEffect<Invigorated>();
        AbilityBase.Revoke<Scp106TargetTeleportAbility>(Player);

        if (notify)
            Player.SendHint($"<color=#aaaaaa>{reason}</color>", 4f);

        if (hunted is not { IsDestroyed: false }) return;

        hunted.DisableEffect<AnomalousTarget>();
        hunted.DisableEffect<Traumatized>();

        if (notify)
            hunted.SendHint("<color=#aaaaaa>SCP-106の追跡から逃れました。</color>", 4f);
    }
}
