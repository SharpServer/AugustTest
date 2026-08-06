using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration;
using Sliced.API.Features;
using UnityEngine;

using Random = UnityEngine.Random;

namespace AntiMeme.Abilities;

/// <summary>
/// 同じゾーン内の、誰にも見られていない場所へ跳びます。
/// </summary>
public sealed class TeleportRandomAbility : AbilityBase
{
    private const float BlackoutDuration = 2.5f;
    private const float FloorOffset = 1.05f;

    /// <summary>
    /// 跳び先にしない部屋です。閉じ込められる部屋と、跳んだ意味が無くなる主要部屋を外しています。
    /// </summary>
    private static readonly HashSet<RoomName> Excluded =
    [
        RoomName.Lcz173,
        RoomName.LczClassDSpawn,
        RoomName.Lcz330,
        RoomName.LczArmory,
        RoomName.LczCheckpointA,
        RoomName.LczCheckpointB,
        RoomName.LczToilets,
        RoomName.Hcz049,
        RoomName.Hcz096,
        RoomName.Hcz106,
        RoomName.Hcz939,
        RoomName.HczArmory,
        RoomName.HczCheckpointA,
        RoomName.HczCheckpointB,
        RoomName.HczCheckpointToEntranceZone,
        RoomName.HczTestroom,
        RoomName.EzCollapsedTunnel,
        RoomName.Outside,
        RoomName.Pocket,
    ];

    /// <inheritdoc/>
    public override string Name => "メインヴィラン";

    /// <inheritdoc/>
    public override string Description => "同じゾーンの、誰も居ない場所へ跳ぶ。";

    /// <inheritdoc/>
    public override float Cooldown => 180f;

    /// <inheritdoc/>
    protected override bool CanUse(out string failureReason)
    {
        if (!base.CanUse(out failureReason)) return false;

        if (Destinations().Any()) return true;

        failureReason = "安全なテレポート位置が見つかりませんでした。";

        return false;
    }

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        List<Vector3> destinations = Destinations().ToList();

        if (destinations.Count == 0) return;

        // 出発側と到着側の両方を暗くして、跳んだ瞬間を見られにくくする。
        Blackout(Player.Room);
        Player.Position = destinations[Random.Range(0, destinations.Count)] + Vector3.up * FloorOffset;
        Blackout(Player.Room);
    }

    /// <summary>
    /// 跳び先の候補です。同じゾーンの空き部屋と、そこに居る味方以外の生存者の足元。
    /// </summary>
    private IEnumerable<Vector3> Destinations()
    {
        FacilityZone zone = Player.Zone;

        // 「生きている非 SCP が居る部屋には跳ばない」を部屋ごとに数え直すと
        // 部屋数 x 人数の総当たりになるので、塞がっている部屋を先に 1 回だけ集める。
        HashSet<Room> occupied =
        [
            .. Player.ReadyList
                .Where(other => other.IsAlive && !other.IsSCP)
                .Select(other => other.Room)
                .Where(room => room is not null),
        ];

        IEnumerable<Vector3> rooms = Room.List
            .Where(room => room.Zone == zone && !Excluded.Contains(room.Name) && !occupied.Contains(room))
            .Select(room => room.Position);

        IEnumerable<Vector3> beside = Player.ReadyList
            .Where(other =>
                !ReferenceEquals(other, Player) &&
                other.IsAlive &&
                other.Room is { } room &&
                !Excluded.Contains(room.Name) &&
                !occupied.Contains(room))
            .Select(other => other.Position);

        return rooms.Concat(beside);
    }

    private static void Blackout(Room room)
    {
        if (room is { LightController: { } lights })
            lights.FlickerLights(BlackoutDuration);
    }
}
