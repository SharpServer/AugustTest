using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Nvg;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerStatsSystem;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;
using AntiMeme.Roles;
using AntiMeme.Roles.FoundationForces;
using AntiMeme.Roles.Scientist;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-3125. A Fifthist-aligned anti-memetic entity that damages nearby
/// non-Fifthist, non-SCP players and disrupts nearby Ara Orun cameras.
/// </summary>
public sealed class Scp3125 : ScpRole
{
    private const float AuraRadius = 2.75f;
    private const float AuraInterval = 2f;
    private const float AuraDamage = 1f;
    private const float CameraDisruptionRadius = 8.75f;
    private const float CameraDisruptionDuration = 5f;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 3 1 2 5";

    public override string Name => "SCP-3125";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff00fa>SCP-3125</color>";

    /// <inheritdoc/>
    public override string Objective => "マリオン・ホイーラーを探し出し第五せよ";

    public override CustomTeam Team => CustomTeam.Get<FifthistTeam>();

    public override string Description =>
        "あなたは反ミーム部門を壊滅させる事に成功した！\n" +
        "残るはかの部門長、<color=#ffa500>マリオンホイーラー</color>を<color=red>殺すだけ</color>だ。";

    public override RoleTypeId BaseRole => RoleTypeId.Scp106;

    public override Vector3? SpawnPosition => SpawnPoints.NtfEntry;

    public override float? MaxHealth => 55555f;

    public override string CustomInfo => "<color=#FF0090>SCP-3125</color>";

    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<Slowness>(30),
    ];

    protected override void OnSpawned()
    {
        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

        Scope.RunLoop(0.5f, _ => UpdateHeadspaceHint());
        Scope.RunLoop(AuraInterval, _ => ApplyAura());
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (IsMine(ev.Player))
        {
            ev.IsAllowed = false;
            return;
        }

        if (ev.Attacker is not { } attacker || !IsMine(attacker.ReferenceHub)) return;

        if (ev.DamageHandler is StandardDamageHandler damage && damage.Damage <= 10f)
            return;

        ev.IsAllowed = false;
    }

    private void ApplyAura()
    {
        foreach (Player target in Player.ReadyList)
        {
            if (IsMine(target) || !target.IsAlive || target.IsSCP || Team.Includes(target))
                continue;

            if ((target.Position - Player.Position).sqrMagnitude > AuraRadius * AuraRadius)
                continue;

            if (target.Items.Any(item => item is Scp1344Item nvg && nvg.IsWorn &&
                                         Sliced.API.Features.CustomItem.Of(item.Serial) is AntiMemeGoggle))
                continue;

            target.Damage(AuraDamage, Player);
            Player.SendHitMarker();
        }

        foreach (Player observer in Player.ReadyList)
        {
            if (observer.RoleBase is not Scp079Role role ||
                CustomRole.Of(observer) is not AraOrun ||
                Vector3.Distance(role.CameraPosition, Player.Position) >= CameraDisruptionRadius)
                continue;

            if (role.SubroutineModule.TryGetSubroutine(out Scp079LostSignalHandler lostSignal))
                lostSignal.ServerLoseSignal(CameraDisruptionDuration);
        }
    }

    private void UpdateHeadspaceHint()
    {
        MarionWheeler marion = CustomRole.Active.OfType<MarionWheeler>().FirstOrDefault();
        if (marion?.Player is not { IsDestroyed: false, IsAlive: true } target)
        {
            ShowStatus("[ヘッドスペース]\n- マリオン・ホイーラー -\n対象を検出できません。", 0.8f);
            return;
        }

        ShowStatus(
            $"[ヘッドスペース]\n- マリオン・ホイーラー -\n階層：{target.Zone}\n距離：{Vector3.Distance(Player.Position, target.Position):F1}m",
            0.8f);
    }
}
