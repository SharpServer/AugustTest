using System.Collections.Generic;
using AntiMeme.Effects;
using AntiMeme.Maps;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-610。攻撃した相手を感染させ、放置されると同族に変えてしまいます。
///
/// 感染者の管理はこのインスタンスが持ちます。SCP-610 が居なくなれば感染の追跡も一緒に消えるので、
/// 旧実装のような「プラグイン全体で共有する感染フラグ」は要りません。
/// カウントダウンは感染者本人の <see cref="PlayerScope"/> に載せるため、
/// 感染者が退出・死亡・役職変更した時点で勝手に止まります。
/// </summary>
public class Scp610 : ScpRole
{
    private const float DamageDivisor = 3.5f;
    private const float InfectedDamageDivisor = 10f;
    private const float InfectionThreshold = 40f;
    private const float InfectionDelay = 200f;

    private readonly HashSet<Player> infected = [];

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 6 1 0";

    public override string Name => "SCP-610";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-610</color>";

    /// <inheritdoc/>
    public override string Objective => "生存者を探し出し、施設をにくで埋め尽くせ";

    public override string Description =>
        "SCP-610に哀れにも感染し、変異してしまった人間の成れの果て。\n" +
        "生存者を攻撃すると感染させ、仲間を増やすことができる。";

    protected override float HintDuration => 15f;

    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;

    public override float? MaxHealth => 800f;

    public override string CustomInfo => "<color=#C50000>SCP-610</color>";

    public override IReadOnlyList<ItemType> Items => [ItemType.SCP1509];

    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.Hcz939, Vector3.up * 0.65f);

    /// <summary>
    /// 素体は Tutorial なので Fade で消し、見た目はスキマティックに任せます。
    /// </summary>
    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<Fade>(255),
        RoleEffect.Of<DamageReduction>(80),

        // 持ち物は SCP-1509 固定。拾い食いも取り落としもさせない。
        RoleEffect.Of<InventoryLocked>(),
    ];

    protected override void OnSpawned()
    {
        Scope.Delay(1.5f, _ =>
        {
            if (CustomRole.Of(Player) is not Scp610) return;
            RoleWear wear = RoleWear.AttachSchematic(Player, "scp-610", Vector3.down);
            if (wear != null) Scope.OnDispose(__ => wear.Dispose());
        });

        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

        Hook(
            () => PlayerEvents.UsedItem += OnUsedItem,
            () => PlayerEvents.UsedItem -= OnUsedItem);
    }

    /// <summary>
    /// SCP-500 が感染の唯一の治療手段です。
    /// </summary>
    private void OnUsedItem(PlayerUsedItemEventArgs ev)
    {
        if (ev.UsableItem.Type is not ItemType.SCP500 || !infected.Remove(ev.Player)) return;

        ev.Player.DisableEffect<Concussed>();
        ev.Player.SendHint("<size=22><color=#00ff88><b>SCP-610の感染が治癒された！</b></color></size>", 5f);
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!IsMine(ev.Attacker) || ev.DamageHandler is not StandardDamageHandler damage) return;

        Player victim = ev.Player;

        if (infected.Contains(victim))
        {
            // 既に感染済みの相手を殴っても旨みは無い。
            damage.Damage /= InfectedDamageDivisor;
            Player.SendHint("<size=24>相手はもうすでに感染しています！</size>");

            return;
        }

        damage.Damage /= DamageDivisor;

        if (victim.Health > InfectionThreshold) return;

        Infect(victim);
    }

    private void Infect(Player victim)
    {
        infected.Add(victim);

        victim.EnableEffect<Concussed>(255);
        victim.EnableEffect<DamageReduction>(60);

        Player.SendHint(
            "<size=22><color=yellow><b>相手を感染させる事に成功した！3分後には同胞になっているであろう！</b></color></size>",
            5f);
        victim.SendHint(
            "<size=22><color=red><b>SCP-610に感染してしまった！\nSCP-500で治療しなければ三分後には同胞になってしまうぞ！</b></color></size>",
            5f);

        // 感染者側のスコープに載せる。退出・ラウンド再開・役職変更で自動的に止まる。
        PlayerScope.Of(victim).Delay(InfectionDelay, target =>
        {
            if (!infected.Remove(target) || !target.IsAlive || target.IsSCP) return;

            CustomRole.Spawn<Scp610>(target);
        });
    }
}
