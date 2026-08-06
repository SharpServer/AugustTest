using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Melee;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.ClassD;

public sealed class Bloodfiend : ClassDRole
{
    public override string Name => "Bloodfiend";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ee7600>Bloodfiend</color>";

    /// <inheritdoc/>
    public override string Objective => "ナイフを使って血を満たせ！";
    public override string Description => "<size=23>代謝および自然治癒能力を向上させる施術を受けたDクラス。\n副作用により強い吸血衝動に苛まれており、医務室で拘束されていたが、\n処分直前に狂暴化して脱走し複数の死傷者を出した。\nなお、異常な能力は確認されておらず、吸血衝動に支配された人間に過ぎない。</size>";
    public override RoleTypeId BaseRole => RoleTypeId.ClassD;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom("EZ_Smallrooms", Vector3.up * 0.8f);
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Bloodfiend";
    public override IReadOnlyList<Type> CustomItems => [ typeof(Bloodyknife) ];

    protected override void OnSpawned()
    {
        Hook(() => PlayerEvents.Hurting += OnHurting, () => PlayerEvents.Hurting -= OnHurting);
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        // 旧実装の DamageBoost は CustomEffects 層に属し、現行 Phase 1 には対応型がありません。

        if (!IsMine(ev.Attacker) || ev.Player is null || !ev.Player.HasEffect<Bleeding>())
            return;

        float healing = ev.DamageHandler is StandardDamageHandler damage && ev.Player.Health - damage.Damage <= 0f
            ? 30f
            : 5f;
        Player.Heal(healing);
    }
}
