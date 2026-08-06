using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Hud;
using AntiMeme.Maps;
using AntiMeme.Maps.Features;
using AntiMeme.Maps.Features.Warhead;
using AntiMeme.Maps.Objects;
using AntiMeme.Spawning;
using AntiMeme.Roles;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Handlers;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using LabApi.Features.Enums;
using MEC;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using ExiledRoom = Exiled.API.Features.Room;
using SystemRandom = System.Random;
using AntiMeme.Spawning.Waves;
using AntiMeme.Roles.Others;
using AntiMeme.Roles.Scientist;

namespace AntiMeme.GameModes.Modes;

/// <summary>
/// 管理官セルゲイ・マカロフの帰還。施設管理官 1 人を残して全員を一度観戦へ落とし、
/// カオス・インサージェンシーとして湧かせ直します。
/// </summary>
/// <remarks>
/// 入れ替えの間は <b>必ずラウンドをロックします</b>。ロックしないと
/// 「セルゲイ 1 人だけが生存」という状態を <c>RoundSummary</c> が拾ってラウンドが終わり、
/// 湧かせ直す前に全員が観戦のまま固定されます。
/// 旧実装が <c>Round.IsLocked = true</c> を掛けっぱなしにしていたのはこのためです。
/// </remarks>
public sealed class SergeyMakarovReturns : RaidMode
{
    public override string Name => "-=[管理官の帰還]=-";

    /// <inheritdoc/>
    public override bool AllowsBreachAnnouncement => false;

    /// <inheritdoc/>
    public override bool AllowsGateLockdown => false;
    public override string Description => "施設管理官セルゲイ・マカロフが帰還する。";
    public override int MinimumPlayers => 8;
    public override int Weight => 66;

    /// <inheritdoc/>
    protected override IReadOnlyList<Season> Seasons => [Season.FifthFestival];

    protected override void OnStarted()
    {
        Round.IsLocked = true;

        Delay(.5f, () =>
        {
            Player target = CustomRole.Active.OfType<FacilityManager>().Select(role => role.Player).FirstOrDefault(IsLiving)
                ?? LivingHumans().FirstOrDefault();

            if (target is null)
            {
                // 誰も立てられないなら入れ替え自体を行わない。ロックは必ず戻す。
                Round.IsLocked = false;

                return;
            }

            CustomRole.Spawn<SergeyMakarovRole>(target);

            Delay(1f, () =>
            {
                foreach (Player player in Living.Where(other => !ReferenceEquals(other, target)).ToArray())
                    player.SetRole(RoleTypeId.Spectator);

                Delay(.5f, () =>
                {
                    SpawnSystem.ForceSpawnNow(SpawnContext.Wave<ChaosInsurgencyWave>());
                    Say("Facility Manager has returned", "施設管理官セルゲイ・マカロフが帰還しました。", true);

                    Round.IsLocked = false;
                });
            });
        });
    }

    /// <inheritdoc/>
    protected override void OnStopped()
    {
        // 入れ替えの途中で打ち切られてもラウンドを閉じ込めない。
        Round.IsLocked = false;

        base.OnStopped();
    }
}
