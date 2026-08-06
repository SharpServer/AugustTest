using System.Collections.Generic;
using AntiMeme.Maps;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-999。勝敗には関わらない愛玩枠です。
///
/// 旧実装は「SCP-173 でスポーン → 遅延して Tutorial へ差し替え → 位置と装備を再適用」という
/// 三段構えでしたが、あれは旧スポーンパイプラインが役職を上書きしてくる分の埋め合わせでした。
/// 新しい <c>CustomRole</c> は宣言どおりに 1 回で組み立てるので、最初から Tutorial です。
/// </summary>
public class Scp999 : ScpRole
{
    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 9 9 9";

    public override string Name => "SCP-999";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff1493>SCP-999</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#c50000>The SCPs</color>";

    /// <inheritdoc/>
    public override string Objective => "可愛いペットとして施設を歩き回れ！　※勝敗に影響しません。良い感じに遊んでね！";

    /// <summary>
    /// どの陣営にも属しません。誰の勝敗にも数えません。
    /// 旧実装はこれを SCP 勝利条件側の除外述語で表現していました。
    /// </summary>
    public override CustomTeam Team => null;

    public override string Description =>
        "全員とたわむれましょう！\n" +
        "※勝敗には影響しません。可愛いペット的にふるまって\n" +
        "攻撃してきた奴らに痛い一撃を喰らわせてやりましょう。";

    protected override float HintDuration => 10f;

    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;

    public override float? MaxHealth => 999f;

    public override string CustomInfo => "<color=#FF1493>SCP-999</color>";

    public override Vector3? Scale => new Vector3(0.35f, 0.2f, 0.35f);

    /// <summary>
    /// 素体は Tutorial なので、見た目は Fade で完全に消してスキマティックに任せます。
    /// </summary>
    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<Fade>(255),
    ];

    protected override void OnSpawned()
    {
        Scope.Delay(1.5f, _ =>
        {
            if (CustomRole.Of(Player) is not Scp999) return;
            RoleWear wear = RoleWear.AttachSchematic(Player, "Scp999Model", Vector3.forward * 0.05f);
            if (wear != null) Scope.OnDispose(__ => wear.Dispose());
        });

        Hook(
            () => PlayerEvents.ShootingWeapon += OnShootingWeapon,
            () => PlayerEvents.ShootingWeapon -= OnShootingWeapon);

        Hook(
            () => PlayerEvents.SpawningRagdoll += OnSpawningRagdoll,
            () => PlayerEvents.SpawningRagdoll -= OnSpawningRagdoll);

        Hook(
            () => PlayerEvents.Dying += OnDying,
            () => PlayerEvents.Dying -= OnDying);
    }

    private void OnShootingWeapon(PlayerShootingWeaponEventArgs ev)
    {
        if (IsMine(ev.Player))
            ev.IsAllowed = false;
    }

    /// <summary>
    /// Tutorial のラグドールが出ると正体がばれるので出しません。
    /// </summary>
    private void OnSpawningRagdoll(PlayerSpawningRagdollEventArgs ev)
    {
        if (IsMine(ev.Player))
            ev.IsAllowed = false;
    }

    private void OnDying(PlayerDyingEventArgs ev)
    {
        if (ev.IsAllowed && IsMine(ev.Player))
            RoleWear.SpawnDeathModel(Player, "Scp999Model");
    }
}
