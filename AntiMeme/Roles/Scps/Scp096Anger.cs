using System.Collections.Generic;
using System.Linq;
using AntiMeme.Audio;
using AntiMeme.Maps.Features.Warhead;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features.Doors;
using Exiled096 = Exiled.API.Features.Roles.Scp096Role;
using ExiledNpc = Exiled.API.Features.Npc;
using ExiledPlayer = Exiled.API.Features.Player;
using ExiledDoor = Exiled.API.Features.Doors.Door;
using ExiledBreakableDoor = Exiled.API.Features.Doors.BreakableDoor;
using ExiledScp096 = Exiled.Events.Handlers.Scp096;
using ExiledAddingTargetEventArgs = Exiled.Events.EventArgs.Scp096.AddingTargetEventArgs;
using ExiledCalmingDownEventArgs = Exiled.Events.EventArgs.Scp096.CalmingDownEventArgs;
using ExiledEnragingEventArgs = Exiled.Events.EventArgs.Scp096.EnragingEventArgs;
using ExiledRemovingTargetEventArgs = Exiled.Events.EventArgs.Scp096.RemovingTargetEventArgs;
using ExiledWarheadDetonatingEventArgs = Exiled.Events.EventArgs.Warhead.DetonatingEventArgs;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-096: Anger. Every living non-SCP observer remains a target until it
/// dies or changes side, so the role cannot be calmed by the vanilla target
/// timer alone.
/// </summary>
public sealed class Scp096Anger : ScpRole
{
    private const float TryNotToCryDuration = 35f;
    private const float RageRefreshDuration = 35f;
    private const float TargetRestoreDelay = 0.05f;
    private const float MaintenanceInterval = 0.2f;
    private const string ChamberGuardName = "SCP-096 Chamber Facility Guard";

    private readonly HashSet<ExiledPlayer> persistentTargets = [];
    private bool inTryNotToCry;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 0 9 6";

    public override string Name => "SCP-096";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-096: ANGER</color>";

    /// <inheritdoc/>
    public override string Objective => "怒りに任せ、施設中で暴れまわれ！！！";

    public override string Description =>
        "<size=24><color=red>SCP-096: Anger</color>\n" +
        "SCP-096の怒りと悲しみが再び不安定化し、本来の力が戻ってきた！\n" +
        "<color=red>自分を見てきた相手を地の底まで追いかけろ！！！</color>";

    public override RoleTypeId BaseRole => RoleTypeId.Scp096;

    public override float? MaxHealth => 8000f;

    public override string CustomInfo => "SCP-096: ANGER";

    protected override void OnSpawned()
    {
        Player.MaxArtificialHealth = 1000f;
        Player.ArtificialHealth = 1000f;
        SetHumeShield(150f);
        BoostHumeShieldRegen(0.35f);
        ChangeSpeedState(false);

        Hook(
            () => ExiledScp096.Enraging += OnEnraging,
            () => ExiledScp096.Enraging -= OnEnraging);

        Hook(
            () => ExiledScp096.AddingTarget += OnTargetAdded,
            () => ExiledScp096.AddingTarget -= OnTargetAdded);

        Hook(
            () => ExiledScp096.RemovingTarget += OnTargetRemoved,
            () => ExiledScp096.RemovingTarget -= OnTargetRemoved);

        Hook(
            () => ExiledScp096.CalmingDown += OnCalmingDown,
            () => ExiledScp096.CalmingDown -= OnCalmingDown);

        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

        Hook(
            () => Exiled.Events.Handlers.Warhead.Detonating += OnVanillaWarheadDetonating,
            () => Exiled.Events.Handlers.Warhead.Detonating -= OnVanillaWarheadDetonating);

        Hook(
            () => OmegaWarhead.Detonating += OnOmegaWarheadDetonating,
            () => OmegaWarhead.Detonating -= OnOmegaWarheadDetonating);

        Scope.RunLoop(MaintenanceInterval, _ => MaintainAnger());
        Scope.Delay(0.5f, _ => StartChamberGuard());
    }

    protected override void OnRemoved()
    {
        persistentTargets.Clear();
        DestroyChamberGuards();

        if (Player is { IsDestroyed: false })
        {
            Player.DisableEffect<DamageReduction>();
            Player.DisableEffect<MovementBoost>();
            Player.DisableEffect<Invigorated>();
            Player.DisableEffect<Slowness>();
        }
    }

    private void OnEnraging(ExiledEnragingEventArgs ev)
    {
        if (!IsMine(ev.Player.ReferenceHub)) return;

        if (inTryNotToCry)
            ev.IsAllowed = false;
        else
            DestroyChamberGuards();
    }

    private void OnTargetAdded(ExiledAddingTargetEventArgs ev)
    {
        if (!IsMine(ev.Player.ReferenceHub) || !ev.IsAllowed || !ShouldPersistTarget(ev.Target))
            return;

        persistentTargets.Add(ev.Target);

        if (ev.Scp096.RageManager.IsEnraged)
        {
            RefreshRageTimer(ev.Scp096);
            ChangeSpeedState(true);
            return;
        }

        if (inTryNotToCry) return;

        inTryNotToCry = true;
        Player.EnableEffect<Slowness>(95);
        Player.EnableEffect<DamageReduction>(90);
        Player.SendHint("<color=red>SCP-096は対象を認識した。35秒後に怒り状態へ移行します。</color>", 5f);
        ExiledPlayer owner = ExiledPlayer.Get(Player.ReferenceHub);
        if (owner != null)
            PlayerSpeakerManager.Play(owner, "096Angered.ogg", "Scp096Angered", isSpatial: false, maxDistance: 80f);

        Scope.Delay(TryNotToCryDuration, _ =>
        {
            inTryNotToCry = false;
            Player.DisableEffect<DamageReduction>();
            PruneTargets();

            if (!TryGetRole(out Exiled096 role) || persistentTargets.Count == 0)
            {
                ChangeSpeedState(false);
                return;
            }

            RestoreMissingTargets(role);
            ChangeSpeedState(true);
            RefreshRageTimer(role);
        });
    }

    private void OnTargetRemoved(ExiledRemovingTargetEventArgs ev)
    {
        if (!IsMine(ev.Player.ReferenceHub) || ev.Target is null) return;

        if (!ShouldPersistTarget(ev.Target))
        {
            persistentTargets.Remove(ev.Target);
            return;
        }

        ev.IsAllowed = false;
        persistentTargets.Add(ev.Target);

        Scope.Delay(TargetRestoreDelay, _ =>
        {
            if (!ShouldPersistTarget(ev.Target) || !TryGetRole(out Exiled096 role)) return;

            RestoreTarget(role, ev.Target);
            if (!inTryNotToCry)
            {
                ChangeSpeedState(true);
                RefreshRageTimer(role);
            }
        });
    }

    private void OnCalmingDown(ExiledCalmingDownEventArgs ev)
    {
        if (!IsMine(ev.Player.ReferenceHub)) return;

        PruneTargets();
        if (persistentTargets.Count == 0)
        {
            ChangeSpeedState(false);
            return;
        }

        ev.IsAllowed = false;
        ev.ShouldClearEnragedTimeLeft = false;
        if (TryGetRole(out Exiled096 role))
        {
            RestoreMissingTargets(role);
            if (!inTryNotToCry)
            {
                ChangeSpeedState(true);
                RefreshRageTimer(role);
            }
        }
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is not { } attacker || !IsMine(attacker.ReferenceHub) ||
            ev.DamageHandler is not PlayerStatsSystem.StandardDamageHandler damage)
            return;

        damage.Damage = 999999f;
        Player.ArtificialHealth += 25f;

        Scope.Delay(1f, _ =>
        {
            PruneTargets();
            if (persistentTargets.Count == 0 && TryGetRole(out Exiled096 role))
            {
                ChangeSpeedState(false);
                role.Calm();
            }
        });
    }

    private void MaintainAnger()
    {
        if (!TryGetRole(out Exiled096 role)) return;

        PruneTargets();
        if (persistentTargets.Count == 0)
        {
            if (!inTryNotToCry && role.RageManager.IsEnraged)
                role.Calm();

            if (!inTryNotToCry)
                ChangeSpeedState(false);

            return;
        }

        RestoreMissingTargets(role);
        if (!inTryNotToCry)
        {
            ChangeSpeedState(true);
            RefreshRageTimer(role);
        }
    }

    private bool TryGetRole(out Exiled096 role)
    {
        role = null;
        if (!IsActive) return false;

        role = ExiledPlayer.Get(Player.ReferenceHub)?.Role as Exiled096;
        return role is not null;
    }

    private void RestoreMissingTargets(Exiled096 role)
    {
        foreach (ExiledPlayer target in persistentTargets.ToArray())
        {
            if (!ShouldPersistTarget(target))
            {
                persistentTargets.Remove(target);
                continue;
            }

            RestoreTarget(role, target);
        }
    }

    private static void RestoreTarget(Exiled096 role, ExiledPlayer target)
    {
        if (!role.Targets.Contains(target))
            role.AddTarget(target);
    }

    private void PruneTargets() => persistentTargets.RemoveWhere(target => !ShouldPersistTarget(target));

    private static bool ShouldPersistTarget(ExiledPlayer target) =>
        target is { IsConnected: true, IsAlive: true } && !target.IsScp;

    private static void RefreshRageTimer(Exiled096 role)
    {
        if (!role.RageManager.IsEnraged)
            role.Enrage(RageRefreshDuration);

        role.TotalEnrageTime = RageRefreshDuration;
        role.EnragedTimeLeft = RageRefreshDuration;
    }

    private void ChangeSpeedState(bool fast)
    {
        if (!IsActive) return;

        if (fast)
        {
            Player.EnableEffect<MovementBoost>(50);
            Player.DisableEffect<Slowness>();
            Player.EnableEffect<Invigorated>(20);
        }
        else
        {
            Player.EnableEffect<Slowness>(40);
            Player.DisableEffect<MovementBoost>();
            Player.DisableEffect<Invigorated>();
        }
    }

    private void StartChamberGuard()
    {
        if (!IsActive) return;

        foreach (ExiledDoor door in ExiledDoor.List)
        {
            if (door.Type == DoorType.HeavyContainmentDoor && door.Room?.Type == RoomType.Hcz096)
                door.Lock(DoorLockType.AdminCommand);
        }

        ExiledPlayer owner = ExiledPlayer.Get(Player.ReferenceHub);
        if (owner == null) return;

        owner.Transform.eulerAngles = new UnityEngine.Vector3(0f, -90f, 0f);
        ExiledNpc guard = ExiledNpc.Spawn(
            ChamberGuardName,
            RoleTypeId.FacilityGuard,
            false,
            position: owner.Position + UnityEngine.Vector3.right);
        if (guard != null)
            guard.Transform.localEulerAngles = new UnityEngine.Vector3(0f, -90f, 0f);
    }

    private void OnVanillaWarheadDetonating(ExiledWarheadDetonatingEventArgs ev)
    {
        if (ev.IsAllowed) DestroyChamberGuards();
    }

    private void OnOmegaWarheadDetonating() => DestroyChamberGuards();

    private static void DestroyChamberGuards()
    {
        foreach (ExiledNpc npc in ExiledNpc.List.ToArray())
        {
            if (string.Equals(npc.CustomName, ChamberGuardName, System.StringComparison.Ordinal))
                npc.Destroy();
        }

        foreach (ExiledDoor door in ExiledDoor.List)
        {
            if (door.Type != DoorType.HeavyContainmentDoor || door.Room?.Type != RoomType.Hcz096)
                continue;

            if (door is ExiledBreakableDoor breakableDoor)
                breakableDoor.Break();
        }
    }

    private bool IsActive => Player is { IsDestroyed: false, IsAlive: true } &&
                              ReferenceEquals(CustomRole.Of(Player), this);
}
