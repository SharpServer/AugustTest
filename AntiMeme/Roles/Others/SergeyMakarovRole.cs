using System;
using System.Collections.Generic;
using AntiMeme.Abilities;
using AntiMeme.Items.Melee;
using AntiMeme.Maps;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MEC;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Others;

public sealed class SergeyMakarovRole : OtherRole
{
    private bool awakenScheduled;

    public override string Name => "施設管理官 - セルゲイ・マカロフ";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#dc143c>Facility Manager - Sergey Makarov</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#faff86>The Foundation</color>";

    /// <inheritdoc/>
    public override string Objective => "持てる全てを使い、<color=#228b22><b>奴ら</b></color>への<color=red><b>復讐</b></color>を果たせ";
    public override string Description => "<size=25>部下に疎まれ、裏切り者に殺され、復讐に憑りつかれ蘇った施設管理官。\n彼は戻ってきた。自身を蔑ろにした全てに復讐するために...\n<b><color=red>持てる全てを使い、奴らへの復讐を果たせ</color></b>";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom("HCZ_IncineratorWayside", new Vector3(0f, 12.55f, 0f));
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Facility Manager";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCrossvec, ItemType.KeycardFacilityManager, ItemType.Medkit,
      ItemType.Medkit, ItemType.ArmorCombat, ItemType.Radio ];
    public override IReadOnlyList<RoleEffect> Effects => [ RoleEffect.Of<Ghostly>() ];

    protected override void OnSpawned()
    {
        Hook(() => PlayerEvents.Dying += OnDying, () => PlayerEvents.Dying -= OnDying);
    }

    private void OnDying(PlayerDyingEventArgs ev)
    {
        if (!IsMine(ev.Player)) return;

        if (awakenScheduled)
        {
            ev.IsAllowed = false;
            return;
        }

        // 旧実装の「死亡を取り消してから怨霊へ目覚める」を、
        // プレイヤースコープに追跡させた遅延へ置き換えます。
        ev.IsAllowed = false;
        Player.IsGodModeEnabled = true;
        Player.Health = Player.MaxHealth;
        awakenScheduled = true;

        Scope.Track(Timing.CallDelayed(19f, () =>
        {
            if (CustomRole.Of(Player) is not SergeyMakarovRole || Player.IsDestroyed)
                return;

            awakenScheduled = false;
            Player.IsGodModeEnabled = false;
            CustomRole.Spawn<SergeyMakarovAwakenRole>(Player);
        }));
    }

    protected override void OnRemoved()
    {
        if (Player is { IsDestroyed: false })
            Player.IsGodModeEnabled = false;
    }
}
