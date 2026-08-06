using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Keycards;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Maps.Core;

/// <summary>
/// <see cref="SpecialDoor"/> の認証を扉のイベントへ繋ぎます。
///
/// <para>
/// 特殊扉は施錠して普通の権限では開かないようにし、条件を満たしたときだけ通します。
/// 条件を満たせない場合でも、Access Tuner Lv3 の特殊アクセスを 1 回消費すれば通れます。
/// </para>
/// </summary>
/// <remarks>
/// 旧実装は規則を <c>Dictionary&lt;string, Rule&gt;</c> に登録する作りでしたが、
/// 登録する側が用意されないまま空の器だけが残っていました。扉 1 枚 = クラス 1 つにすれば
/// 宣言した時点で載るので、登録の口そのものが要りません。
/// </remarks>
public sealed class SpecialDoorHandler : MapFeature
{
    /// <summary>宣言されている特殊扉です。継承するだけで並びます。</summary>
    private static readonly IReadOnlyList<SpecialDoor> Doors =
        TypeParser.FindTypes<SpecialDoor>()
            .Select(type => (SpecialDoor)Activator.CreateInstance(type))
            .ToArray();

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        PlayerEvents.InteractingDoor += OnInteractingDoor;
        ServerEvents.DoorDamaging += OnDoorDamaging;
        ServerEvents.RoundStarted += ApplyDoorState;
        ServerEvents.WaitingForPlayers += ApplyDoorState;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        PlayerEvents.InteractingDoor -= OnInteractingDoor;
        ServerEvents.DoorDamaging -= OnDoorDamaging;
        ServerEvents.RoundStarted -= ApplyDoorState;
        ServerEvents.WaitingForPlayers -= ApplyDoorState;
    }

    /// <summary>
    /// この位置にある特殊扉です。無ければ null。
    /// </summary>
    public static SpecialDoor Of(Door door) =>
        door is null ? null : Doors.FirstOrDefault(special => special.IsAt(door.Position));

    /// <summary>
    /// この扉を壊してよいか。特殊扉は壊せません。
    /// </summary>
    public static bool CanBreak(Door door) => door is not null && Of(door) is null;

    /// <summary>
    /// 扉の初期状態を整えます。特殊扉は施錠し、地上ゲートは全権限必須、脱出扉は解錠します。
    /// </summary>
    private static void ApplyDoorState()
    {
        foreach (Door door in Door.List)
        {
            switch (door.DoorName)
            {
                case DoorName.SurfaceGate:
                    door.Base.RequiredPermissions.RequireAll = true;
                    door.Permissions = DoorPermissionFlags.ExitGates;
                    break;

                case DoorName.SurfaceEscapeFinal:
                    door.Lock(DoorLockReason.AdminCommand, false);
                    break;

                default:
                    if (Of(door) is not null)
                        door.Lock(DoorLockReason.AdminCommand, true);

                    break;
            }
        }
    }

    private static void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        if (Of(ev.Door) is not { } special) return;

        ev.IsAllowed = special.CanOpen(ev.Player) || TryBypass(ev.Player);

        if (!ev.IsAllowed)
        {
            ev.Player.SendHint(
                $"{special.DeniedHint}\n" +
                "<size=22><color=yellow>※ヒントはその辺に落ちている、インタラクトできる報告書などに書いてある事があるよ！</color></size>",
                5f);
        }
    }

    /// <summary>認証を迂回されないよう、特殊扉は壊せないようにします。</summary>
    private static void OnDoorDamaging(DoorDamagingEventArgs ev)
    {
        if (!CanBreak(ev.Door))
            ev.IsAllowed = false;
    }

    /// <summary>
    /// Access Tuner Lv3 の特殊アクセスで無理やり通します。1 回ぶん消費します。
    /// </summary>
    private static bool TryBypass(Player player) =>
        player.Items.Any(item =>
            CustomItem.Of(item.Serial) is AccessTuner tuner && tuner.TryConsumeSpecialAccess(player));
}
