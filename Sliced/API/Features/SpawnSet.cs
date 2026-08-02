using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Extensions;
using Sliced.API.Structs;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace Sliced.API.Features;

public abstract class SpawnSet
{
    public abstract string Id { get; set; }

    public abstract string Name { get; set; }

    public abstract string Description { get; set; }

    /// <summary>
    /// このSpawnSetが割り当て可能な最大プレイヤー数です。
    /// -1の場合、対象プレイヤーがいる限り割り当てます。
    /// </summary>
    public virtual int AllowedPlayerCount { get; set; } = -1;

    public virtual Vector3? OverridePosition { get; set; }

    public abstract IReadOnlyList<SpawnSetRoleDefinition> SpawnRoles { get; set; }

    /// <summary>
    /// このSpawnSetの割り当て対象になりうるプレイヤーです。
    /// 既定では「まだ役職が割り当てられていないプレイヤー」を返します。
    ///
    /// ラウンド開始時 (LabApiのRoundStarted) はバニラの役職割り当て
    /// (PlayerRoles.RoleAssign.RoleAssigner) より前に発火するため、
    /// その時点のプレイヤーはSpectatorではなくRoleTypeId.None (ロビー) である。
    /// ラウンド中に呼ばれた場合はSpectatorが対象になる。
    /// Overwatchは RoleTypeId.Overwatch なのでここには含まれない。
    /// </summary>
    protected virtual List<Player> TargetPlayers()
    {
        return Player.ReadyList
            .Where(player => player.Role is RoleTypeId.None or RoleTypeId.Spectator)
            .Shuffled();
    }

    public void Spawn()
    {
        bool alreadyLocked = Round.IsLocked;

        if (!alreadyLocked)
            Round.IsLocked = true;

        // 途中でreturnしてもラウンドロックを必ず戻す。
        try
        {
            SpawnInternal();
        }
        finally
        {
            if (!alreadyLocked)
                Round.IsLocked = false;
        }
    }

    private void SpawnInternal()
    {
        if (AllowedPlayerCount < -1)
        {
            Logger.Error(
                $"SpawnSet '{Id}' has an invalid AllowedPlayerCount: " +
                $"{AllowedPlayerCount}. The value must be -1 or greater.");

            return;
        }

        if (SpawnRoles is null || SpawnRoles.Count == 0)
            return;

        List<Player> processPlayers = TargetPlayers();

        if (processPlayers.Count == 0 || AllowedPlayerCount == 0)
            return;

        // TargetPlayers()はすでにシャッフル済みなので、
        // 先頭から必要人数を取り出すだけでランダム選出になる。
        if (AllowedPlayerCount > 0 &&
            processPlayers.Count > AllowedPlayerCount)
        {
            processPlayers = processPlayers
                .Take(AllowedPlayerCount)
                .ToList();
        }

        List<SpawnRoleState> roleStates = SpawnRoles
            .Shuffled()
            .Select(role => new SpawnRoleState(role))
            .ToList();

        List<SpawnRoleState> forcedRoles = roleStates
            .Where(state => state.Definition.IsForced)
            .ToList();

        List<SpawnRoleState> normalRoles = roleStates
            .Where(state => !state.Definition.IsForced)
            .ToList();

        SpawnRoutine(forcedRoles, processPlayers);
        SpawnRoutine(normalRoles, processPlayers);
    }

    private void SpawnRoutine(
        IReadOnlyList<SpawnRoleState> roleStates,
        List<Player> processPlayers)
    {
        if (roleStates.Count == 0)
            return;

        while (processPlayers.Count > 0)
        {
            bool spawnedInCurrentPass = false;

            foreach (SpawnRoleState state in roleStates)
            {
                if (processPlayers.Count == 0)
                    return;

                SpawnSetRoleDefinition role = state.Definition;

                if (state.IsInvalid)
                    continue;

                if (role.RoleAllowedCount <= state.ProcessedCount)
                    continue;

                if (!IsValidDefinition(role))
                {
                    state.IsInvalid = true;

                    Logger.Error(
                        $"SpawnSet '{Id}' contains an invalid role definition. " +
                        $"CustomRole is null, but VanillaRole is CustomRole.");

                    continue;
                }

                Player target = processPlayers.RandomItem();

                processPlayers.Remove(target);

                if (role.CustomRole is not null)
                {
                    role.CustomRole.Spawn(target);
                }
                else
                {
                    target.SetRole(role.VanillaRole);
                }

                state.ProcessedCount++;
                spawnedInCurrentPass = true;

                if (OverridePosition is { } overridePosition)
                {
                    target.Position = overridePosition;
                }
            }

            // すべてのRoleが上限到達済み、または無効だった場合。
            if (!spawnedInCurrentPass)
                break;
        }
    }

    private static bool IsValidDefinition(
        SpawnSetRoleDefinition definition)
    {
        return definition.CustomRole is not null ||
               definition.VanillaRole is not RoleTypeId.CustomRole;
    }

    private sealed class SpawnRoleState(
        SpawnSetRoleDefinition definition)
    {
        public SpawnSetRoleDefinition Definition { get; } = definition;

        public int ProcessedCount { get; set; }

        public bool IsInvalid { get; set; }
    }
}