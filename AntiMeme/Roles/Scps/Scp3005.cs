using System.Collections.Generic;
using System.Linq;
using AntiMeme.Abilities;
using AntiMeme.Hud;
using AntiMeme.Items.Nvg;
using AntiMeme.Maps;
using AntiMeme.Maps.Features;
using AntiMeme.Roles.Fifthist;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp0492Events;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;

// マップ機能側にも Sinkhole があるので、効果のほうを明示して指す。
using SinkholeEffect = CustomPlayerEffects.Sinkhole;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-3005。触れた者を焼く第五的な光で、通常の攻撃では傷つきません。
///
/// <para>
/// 攻撃を弾かない例外は 2 つ。<c>Sinkhole</c> (陥没穴に落ちている状態) のときと、
/// 攻撃側が <see cref="AntiMemeGoggle"/> を装着しているときです。
/// </para>
/// <para>
/// アンチミームプロトコル (<see cref="AntiMemeProtocolFunction"/>) が一度でも作動すると
/// 加速し、作動中は毎周期ダメージを受け続けます。
/// </para>
/// </summary>
public class Scp3005 : ScpRole
{
    private const float AuraRadius = 2.75f;
    private const float AuraInterval = 1.5f;
    private const float AuraDamage = 25f;
    private const float ReflectDamage = 20f;
    private const float ProtocolDamage = 100f;
    private const byte SlowIntensity = 25;
    private const byte BoostIntensity = 25;
    private const string DamageReason = "<color=#ff00fa>第五的</color>な力による影響";
    private const string ProtocolReason = "<color=#ff00fa>アンチミームプロトコル</color>により終了された";

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 3 0 0 5";

    public override string Name => "SCP-3005";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff00fa>SCP-3005</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#c50000>The SCPs</color> - <color=#ff00fa>The Fifthists</color>";

    /// <inheritdoc/>
    public override string Objective => "第五教会に道を示し、施設を占領せよ";

    /// <summary>
    /// SCP でありながら第五教会に属します。
    /// 旧実装はこれを SCP 勝利条件側の除外述語で表現していましたが、
    /// 役職が自分で名乗れば済みます。
    /// </summary>
    public override CustomTeam Team => CustomTeam.Get<FifthistTeam>();

    public override string Description =>
        "第五的なピンクの光を放つ、謎に包まれた存在。\n" +
        "<color=#ff00fa>第五教会に道を示し、施設を第五せよ！</color>";

    protected override float HintDuration => 8f;

    public override RoleTypeId BaseRole => RoleTypeId.Scp0492;

    public override float? MaxHealth => 55556f;

    public override string CustomInfo => "SCP-3005";

    public override Vector3? Scale => new Vector3(0.01f, 1f, 0.01f);

    /// <summary>
    /// マップ側のマーカーで指定します。マーカーはマップ生成後にしか引けないので都度評価します。
    /// </summary>
    public override Vector3? SpawnPosition => SpawnPoints.Tagged("Scp3005SpawnPoint");

    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<MovementBoost>(50),
    ];

    protected override void OnSpawned()
    {
        // 満タンのまま出さない。旧実装から引き継いでいる「常に 1 減っている」状態。
        Player.Health = Player.MaxHealth - 1f;

        AbilityBase.Give<MindblasterAbility>(Player);
        AbilityBase.Give<SoundOfFifthAbility>(Player);

        Scope.Delay(1.5f, _ =>
        {
            if (CustomRole.Of(Player) is not Scp3005) return;

            RoleWear wear = RoleWear.AttachSchematic(Player, "SCP3005");

            if (wear is null) return;

            wear.WithLight(Color.magenta);
            Scope.OnDispose(__ => wear.Dispose());
        });

        Scope.RunLoop(AuraInterval, _ => Tick());

        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

        Hook(
            () => PlayerEvents.SpawningRagdoll += OnSpawningRagdoll,
            () => PlayerEvents.SpawningRagdoll -= OnSpawningRagdoll);

        Hook(
            () => PlayerEvents.Dying += OnDying,
            () => PlayerEvents.Dying -= OnDying);

        Hook(
            () => Scp0492Events.ConsumedCorpse += OnConsumedCorpse,
            () => Scp0492Events.ConsumedCorpse -= OnConsumedCorpse);
    }

    private void Tick()
    {
        BurnNearby();
        ApplyProtocolState();
    }

    private void BurnNearby()
    {
        foreach (Player target in Player.ReadyList)
        {
            if (IsMine(target) || !target.IsAlive) continue;

            // 味方 (SCP・第五教会) と、反ミーム対策ゴーグルを着けた相手には効かない。
            if (target.IsSCP || CustomTeam.Of(target) is FifthistTeam) continue;
            if (WearsAntiMemeGoggle(target)) continue;

            if ((target.Position - Player.Position).sqrMagnitude > AuraRadius * AuraRadius) continue;

            target.Damage(AuraDamage, Player);
            Player.SendHitMarker();
        }
    }

    /// <summary>
    /// アンチミームプロトコルの状態を反映します。
    /// 一度でも作動していれば加速し、していなければ鈍足のままです。作動中は削られ続けます。
    /// </summary>
    private void ApplyProtocolState()
    {
        if (AntiMemeProtocolFunction.HasActivatedInPast)
        {
            Player.DisableEffect<Slowness>();
            Player.EnableEffect<MovementBoost>(BoostIntensity);
        }
        else
        {
            Player.DisableEffect<MovementBoost>();
            Player.EnableEffect<Slowness>(SlowIntensity);
        }

        if (AntiMemeProtocolFunction.IsActive)
            Player.Damage(ProtocolDamage, ProtocolReason);
    }

    /// <summary>
    /// 自分への攻撃は跳ね返します。陥没穴に落ちている間と、
    /// 反ミーム対策ゴーグルを着けた相手からの攻撃だけは通ります。
    /// </summary>
    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!IsMine(ev.Player)) return;
        if (ev.Attacker is not { } attacker || IsMine(attacker)) return;
        if (Player.HasEffect<SinkholeEffect>() || WearsAntiMemeGoggle(attacker)) return;

        ev.IsAllowed = false;
        attacker.Damage(ReflectDamage, DamageReason);

        if (CustomTeam.Of(attacker) is FifthistTeam)
            attacker.SendHint("第五に反逆するとは何事か！？", 3f);
    }

    /// <summary>
    /// 食べた死体は第五教会の操り人形として起き上がります。回復はしません。
    /// </summary>
    private void OnConsumedCorpse(Scp0492ConsumedCorpseEventArgs ev)
    {
        if (!IsMine(ev.Player) || ev.Ragdoll is not { } ragdoll) return;
        if (Player.Get(ragdoll.Base.Info.OwnerHub) is not { IsDestroyed: false } owner || owner.IsAlive) return;

        Vector3 position = ragdoll.Position + Vector3.up * 0.15f;

        CustomRole.Spawn<FifthistMarionetteRole>(owner);
        owner.Position = position;
    }

    private void OnSpawningRagdoll(PlayerSpawningRagdollEventArgs ev)
    {
        if (IsMine(ev.Player))
            ev.IsAllowed = false;
    }

    private void OnDying(PlayerDyingEventArgs ev)
    {
        if (ev.IsAllowed && IsMine(ev.Player))
            RoleWear.SpawnDeathModel(Player, "SCP3005_N");
    }

    /// <inheritdoc/>
    protected internal override void AnnounceTermination(PlayerDyingEventArgs ev)
    {
        // プロトコル作動中に攻撃者なしで死んだのなら、無力化されたということ。
        if (AntiMemeProtocolFunction.IsActive && ev.Attacker is null)
        {
            FacilityAnnouncer.Terminate(
                CassieName,
                $"<color={Team.Color}>{Name}</color>",
                TerminationCause.AntiMeme(),
                clear: true);

            return;
        }

        base.AnnounceTermination(ev);
    }

    private static bool WearsAntiMemeGoggle(Player player) =>
        player.Items.Any(item =>
            item is Scp1344Item { IsWorn: true } && CustomItem.Of(item.Serial) is AntiMemeGoggle);
}
