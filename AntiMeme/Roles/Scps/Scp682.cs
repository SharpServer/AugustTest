using AntiMeme.Roles;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp939Events;
using LabApi.Events.Handlers;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Structs;
using UnityEngine;

using ExiledScp939 = Exiled.Events.Handlers.Scp939;
using PlacingMimicPointEventArgs = Exiled.Events.EventArgs.Scp939.PlacingMimicPointEventArgs;
using PlayingFootstepEventArgs = Exiled.Events.EventArgs.Scp939.PlayingFootstepEventArgs;
using PlayingVoiceEventArgs = Exiled.Events.EventArgs.Scp939.PlayingVoiceEventArgs;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-682。SCP-939 を土台にしていますが、939 の「音を真似る」系の能力は全部潰してあります。
///
/// 目覚めるほど強くなる役職で、覚醒度は時間経過と与ダメージの両方で上がり、
/// そのまま攻撃倍率になります。覚醒度はこのインスタンスのフィールドです。
/// </summary>
public class Scp682 : ScpRole
{
    private const float AwakenPerSecond = 1.0005f;
    private const float AwakenPerDamage = 1f / 10000f;
    private const float StatusInterval = 1f;

    private float awaken = 1f;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 6 8 2";

    public override string Name => "SCP-682";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-682</color>";

    /// <inheritdoc/>
    public override string Objective => "無敵の爬虫類の力を見せてやれ！！！";

    public override string Description =>
        "不死身の爬虫類とまで恐れられた最強クラスのSCP。\n" +
        "その危険性から長い間眠らされていたが、大規模な収容違反の影響により\n" +
        "遂に目覚めることができた。今まで抑え込まれていた物を全て解き放ち、\n" +
        "<color=red>忌まわしき財団を破壊せよ！</color>";

    public override RoleTypeId BaseRole => RoleTypeId.Scp939;

    /// <summary>マップ側のマーカーで指定します。マーカーが無ければバニラの地点です。</summary>
    public override Vector3? SpawnPosition => SpawnPoints.Tagged("Scp682SpawnPoint");

    public override float? MaxHealth => 999f;

    public override string CustomInfo => "SCP-682";

    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<FocusedVision>(255),
        RoleEffect.Of<NightVision>(255),
    ];

    public override Vector3? Scale => new Vector3(0.7f, 0.75f, 1.2f);

    protected override void OnSpawned()
    {
        SetHumeShield(1200f);
        BoostHumeShieldRegen(13.5f);

        Scope.RunLoop(StatusInterval, _ =>
        {
            awaken *= AwakenPerSecond;
            ShowStatus($"Awaken Status: {awaken:F2}", StatusInterval + 0.2f);
        });

        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

        Hook(
            () => Scp939Events.CreatingAmnesticCloud += OnCreatingAmnesticCloud,
            () => Scp939Events.CreatingAmnesticCloud -= OnCreatingAmnesticCloud);

        Hook(
            () => Scp939Events.MimickingEnvironment += OnMimickingEnvironment,
            () => Scp939Events.MimickingEnvironment -= OnMimickingEnvironment);

        // 足音・肉声の模倣とミミックポイント設置は LabApi にイベントが無いので EXILED 側で止める。
        Hook(
            () => ExiledScp939.PlayingFootstep += OnPlayingFootstep,
            () => ExiledScp939.PlayingFootstep -= OnPlayingFootstep);

        Hook(
            () => ExiledScp939.PlayingVoice += OnPlayingVoice,
            () => ExiledScp939.PlayingVoice -= OnPlayingVoice);

        Hook(
            () => ExiledScp939.PlacingMimicPoint += OnPlacingMimicPoint,
            () => ExiledScp939.PlacingMimicPoint -= OnPlacingMimicPoint);
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!IsMine(ev.Attacker) || ev.DamageHandler is not StandardDamageHandler damage) return;

        damage.Damage *= awaken;
        awaken += damage.Damage * AwakenPerDamage;
    }

    private void OnCreatingAmnesticCloud(Scp939CreatingAmnesticCloudEventArgs ev)
    {
        if (IsMine(ev.Player))
            ev.IsAllowed = false;
    }

    private void OnMimickingEnvironment(Scp939MimickingEnvironmentEventArgs ev)
    {
        if (IsMine(ev.Player))
            ev.IsAllowed = false;
    }

    private void OnPlayingFootstep(PlayingFootstepEventArgs ev)
    {
        if (IsMine(ev.Player.ReferenceHub))
            ev.IsAllowed = false;
    }

    private void OnPlayingVoice(PlayingVoiceEventArgs ev)
    {
        if (IsMine(ev.Player.ReferenceHub))
            ev.IsAllowed = false;
    }

    private void OnPlacingMimicPoint(PlacingMimicPointEventArgs ev)
    {
        if (IsMine(ev.Player.ReferenceHub))
            ev.IsAllowed = false;
    }
}
