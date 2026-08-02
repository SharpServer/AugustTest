using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Interfaces;
using UnityEngine;

namespace Sliced.API.Features;

public abstract class CustomRole : IPlayerOwn
{
    /// <summary>
    /// Owner player of this CustomRole instance.
    /// </summary>
    public Player Player { get; private set; } = null;
    
    /// <summary>
    /// This CustomRole's displayed Name for players.
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// This CustomRole's description for players.
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// Player spawns role type.
    /// </summary>
    public abstract RoleTypeId SpawnRoleType { get; protected set; }
    
    /// <summary>
    /// Player spawns position.
    /// </summary>
    public abstract Vector3 SpawnPosition { get; protected set; }

    /// <summary>
    /// Normal Spawn method for this CustomRole.
    /// </summary>
    /// <param name="player">Spawn target player.</param>
    /// <param name="roleSpawnFlags">RoleSpawnFlags for this CustomRole spawning.</param>
    /// <returns>Return true if this spawn is successfully executed.</returns>
    public bool Spawn(Player player, RoleSpawnFlags roleSpawnFlags = RoleSpawnFlags.All)
    {
        Player = player;
        if (Player is null || !Player.IsReady) return false;
        if (roleSpawnFlags is RoleSpawnFlags.All or (RoleSpawnFlags.AssignInventory | RoleSpawnFlags.UseSpawnpoint))
        {
            Player.SetRole(SpawnRoleType);
            Player.Position = SpawnPosition;
            InventoryInit();
        }
        else if (roleSpawnFlags is RoleSpawnFlags.AssignInventory)
        {
            Player.SetRole(SpawnRoleType, flags: RoleSpawnFlags.None);
            InventoryInit();
        }
        else if (roleSpawnFlags is RoleSpawnFlags.UseSpawnpoint)
        {
            Player.SetRole(SpawnRoleType, flags: RoleSpawnFlags.None);
            Player.Position = SpawnPosition;
            InventoryInit(false);
        }
        else
        {
            Player.SetRole(SpawnRoleType, flags: RoleSpawnFlags.None);
            InventoryInit(false);
        }

        if (Player.Role == SpawnRoleType)
        {
            OnSpawned();
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Init Player's inventory.
    /// </summary>
    /// <param name="isOverride"></param>
    private void InventoryInit(bool isOverride = true)
    {
        Player.ClearInventory();
        if (!isOverride) return;
        OnInventoryInit();
    }

    /// <summary>
    /// It's called on this CustomRole's Inventory Initialization Process.
    /// Override this method if you want to give some items.
    /// </summary>
    protected virtual void OnInventoryInit() {}
    
    /// <summary>
    /// Override this for Spawn Initialization.
    /// </summary>
    protected virtual void OnSpawned() {}
}