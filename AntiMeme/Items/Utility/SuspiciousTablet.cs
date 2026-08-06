using System;
using System.Collections.Generic;
using AntiMeme.Items;
using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using AntiMeme.Teams.Factions;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;
using ExiledRound = Exiled.API.Features.Round;

namespace AntiMeme.Items.Utility;

/// <summary>使用者を仮死状態にし、二分後に元の役職へ戻す薬です。</summary>
public sealed class SuspiciousTablet : CustomUsable, IScp914Upgradable
{
    /// <summary>1:1 に通したときに詰め直すキャンディの個数です。</summary>
    private const int RepackedCandyCount = 6;

    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.Painkillers),
        OneToOne = Scp914Rule.Custom(RepackAsCandyBag),
        Fine = Scp914Rule.To<ClassBMemoryRemovePill>(),
        VeryFine = Scp914Rule.To<LsdPillSafe>(),
    };

    /// <summary>
    /// SCP-330 の袋に差し替え、中身をピンクだけで満たします。
    /// 床に置かれていた場合は袋を詰め直す相手がいないので、素の SCP-330 を出すだけにします。
    /// </summary>
    private static void RepackAsCandyBag(Scp914Context context)
    {
        Player owner = context.Owner;

        context.Consume();
        context.Deliver(ItemType.SCP330);

        if (owner is not { IsDestroyed: false }) return;
        if (!Scp330Bag.TryGetBag(owner.ReferenceHub, out Scp330Bag bag)) return;

        bag.Candies.Clear();

        for (int index = 0; index < RepackedCandyCount; index++)
            bag.TryAddSpecific(CandyKindID.Pink);

        bag.ServerRefreshBag();
    }

    private const float RespawnTime = 120f;
    private static bool hooked;
    private static readonly Dictionary<Player, Pending> PendingPlayers = new();

    public SuspiciousTablet() => Hook();

    public override ItemType BaseType => ItemType.Adrenaline;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;
    public override string Name => "怪しい薬";
    public override string Description => "誰でも 100% 安心・安全に一時的な死亡を体験できる薬。";

    protected override bool CanUse(Player player) => !CustomTeam.Get<ScpTeam>().Includes(player);

    protected override void OnUse(Player player)
    {
        CustomRole role = CustomRole.Of(player);
        PendingPlayers[player] = new Pending(player.Position, player.Role, role?.GetType());
        player.SendHint($"<color=yellow>※{RespawnTime:0}秒後に復活します。</color>", 8f);
        player.Kill("薬によって仮死状態に至った！");
    }

    private static void OnDeath(PlayerDeathEventArgs ev)
    {
        if (!PendingPlayers.TryGetValue(ev.Player, out Pending pending)) return;
        PendingPlayers.Remove(ev.Player);

        RoundScope.Current.Delay(RespawnTime, () =>
        {
            Player player = ev.Player;
            if (player is not { IsDestroyed: false } || player.IsAlive || ExiledRound.IsLobby) return;

            if (pending.CustomRole is not null)
                CustomRole.Spawn(pending.CustomRole, player);
            else
                player.SetRole(pending.Role, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.AssignInventory);

            if (player is { IsDestroyed: false, IsAlive: true })
            {
                player.Position = pending.Position + Vector3.up * 1.05f;
                player.Health = player.MaxHealth;
            }
        });
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.Death += OnDeath;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.Death -= OnDeath;
            PendingPlayers.Clear();
            hooked = false;
        });
    }

    private readonly struct Pending
    {
        public Pending(Vector3 position, RoleTypeId role, Type customRole)
        {
            Position = position;
            Role = role;
            CustomRole = customRole;
        }

        public Vector3 Position { get; }
        public RoleTypeId Role { get; }
        public Type CustomRole { get; }
    }
}
