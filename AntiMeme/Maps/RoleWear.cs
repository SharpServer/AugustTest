using System;
using LabApi.Features.Wrappers;
using MEC;
using ProjectMER.Features;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
using UnityEngine;
using ExiledCapybara = Exiled.API.Features.Toys.Capybara;
using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Maps;

/// <summary>
/// Attaches ProjectMER schematics and simple admin toys to a player's transform.
/// The returned handle owns every spawned object and must live in the role's
/// <see cref="Sliced.API.Features.PlayerScope"/>.
/// </summary>
public sealed class RoleWear : IDisposable
{
    private SchematicObject schematic;
    private LightSourceToy light;
    private ExiledCapybara capybara;
    private bool disposed;

    private RoleWear()
    {
    }

    public SchematicObject Schematic => schematic;

    /// <summary>
    /// スキマティックをプレイヤーに追従させます。
    /// </summary>
    /// <param name="hideFromWearer">
    /// 着ている本人から隠すかどうか。既定は true です。
    /// </param>
    /// <remarks>
    /// <b>本人には見せません。</b> 着ぐるみは他人に見せるためのもので、
    /// 一人称視点では自分の足元に model が突き刺さって視界の邪魔になるだけです。
    /// 旧実装も <c>TryWear</c> の直後に <c>DestroySchematic</c> を呼んで
    /// 着用者の分だけ削っていました (SCP-035 / SCP-610 / Initiative Wolf)。
    /// </remarks>
    public static RoleWear AttachSchematic(
        Player player,
        string schematicName,
        Vector3 offset = default,
        Vector3? scale = null,
        Quaternion? rotationOffset = null,
        bool hideFromWearer = true)
    {
        if (player is null || player.IsDestroyed || string.IsNullOrWhiteSpace(schematicName)) return null;

        try
        {
            Transform playerTransform = player.ReferenceHub.transform;
            Vector3 position = player.Position + playerTransform.TransformDirection(offset);
            SchematicObject spawned = ObjectSpawner.SpawnSchematic(schematicName, position, player.Rotation);
            if (spawned is null) return null;

            if (scale.HasValue)
                spawned.Scale = scale.Value;

            RoleWearFollower follower = spawned.gameObject.AddComponent<RoleWearFollower>();
            follower.Initialize(playerTransform, offset, rotationOffset);

            if (hideFromWearer)
            {
                // NetworkIdentity は生成直後には揃っていないことがあるので、次フレームに削る。
                Timing.CallDelayed(0f, () =>
                {
                    if (player is { IsDestroyed: false } && spawned != null)
                        player.DestroySchematic(spawned);
                });
            }

            return new RoleWear { schematic = spawned };
        }
        catch (Exception exception)
        {
            Logger.Warn($"[RoleWear] Could not attach schematic '{schematicName}': {exception.Message}");
            return null;
        }
    }

    public static RoleWear AttachCapybara(Player player)
    {
        if (player is null || player.IsDestroyed) return null;

        try
        {
            ExiledCapybara spawned = ExiledCapybara.Create(player.Position, player.Rotation);
            if (spawned is null) return null;

            spawned.Collidable = false;
            RoleWearFollower follower = spawned.GameObject.AddComponent<RoleWearFollower>();
            follower.Initialize(player.ReferenceHub.transform, Vector3.zero, null);

            return new RoleWear { capybara = spawned };
        }
        catch (Exception exception)
        {
            Logger.Warn($"[RoleWear] Could not attach capybara: {exception.Message}");
            return null;
        }
    }

    public RoleWear WithLight(Color color, float range = 10f, float intensity = 1.25f, Vector3 offset = default)
    {
        if (disposed || (schematic is null && capybara is null)) return this;

        try
        {
            Transform parent = schematic?.transform ?? capybara.GameObject.transform;
            light = LightSourceToy.Create(parent.position + offset, parent.rotation, parent, false);
            if (light is null) return this;

            light.Color = color;
            light.Range = range;
            light.Intensity = intensity;
            light.Spawn();
        }
        catch (Exception exception)
        {
            Logger.Warn($"[RoleWear] Could not attach light: {exception.Message}");
        }

        return this;
    }

    public static void SpawnDeathModel(Player player, string schematicName, Vector3? scale = null)
    {
        if (player is null || player.IsDestroyed || string.IsNullOrWhiteSpace(schematicName)) return;

        try
        {
            ObjectSpawner.SpawnSchematic(
                schematicName,
                player.Position,
                player.Rotation,
                scale ?? Vector3.one);
        }
        catch (Exception exception)
        {
            Logger.Warn($"[RoleWear] Could not spawn death model '{schematicName}': {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        try { light?.Destroy(); } catch { }
        try { schematic?.Destroy(); } catch { }
        try { capybara?.Destroy(); } catch { }

        light = null;
        schematic = null;
        capybara = null;
    }
}

/// <summary>Updates a worn object's root without parenting networked objects.</summary>
public sealed class RoleWearFollower : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private Quaternion rotationOffset = Quaternion.identity;

    public void Initialize(Transform owner, Vector3 localOffset, Quaternion? localRotation)
    {
        target = owner;
        offset = localOffset;
        rotationOffset = localRotation ?? Quaternion.identity;
    }

    private void Update()
    {
        if (target is null)
        {
            Destroy(this);
            return;
        }

        transform.position = target.position + target.TransformDirection(offset);
        transform.rotation = target.rotation * rotationOffset;
    }
}
