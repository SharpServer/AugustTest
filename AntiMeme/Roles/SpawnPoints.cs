using System;
using System.Linq;
using AntiMeme.Maps;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using MapGeneration;
using UnityEngine;

namespace AntiMeme.Roles;

/// <summary>
/// 役職のスポーン地点を引くための小さな道具箱です。
///
/// <para>
/// 旧実装は <c>PositionProvider</c> (座標 3 個の関数) と <c>MapFlags</c> (Vector3 フィールド 30 個) と
/// <c>StaticUtils.GetWorldFromRoomLocal</c> に分かれていて、さらに各役職が
/// <c>RoleSpawnTimings</c> の待ち時間つき <c>Timing.CallDelayed</c> で位置を後から書いていました。
/// <see cref="Sliced.API.Features.CustomRole.SpawnPosition"/> はスポーンの中で 1 回だけ評価されるので、
/// 「引ければ座標、引けなければ null (= バニラのスポーン地点)」を返せば足ります。
/// </para>
/// <para>
/// <b>フィールド初期化子で呼ばないでください。</b> 部屋もマーカーもマップ生成後にしか引けません。
/// </para>
/// </summary>
public static class SpawnPoints
{
    /// <summary>
    /// 地上の機動部隊降下地点です。
    /// </summary>
    public static Vector3 NtfEntry => new Vector3(127f, 295.5f, -40f);

    /// <summary>
    /// 地上のカオス侵入地点です。
    /// </summary>
    public static Vector3 ChaosEntry => new Vector3(8f, 292f, -45f);

    /// <summary>
    /// ゲート B の外です。第五教会の集合地点として使います。
    /// </summary>
    public static Vector3 ExitB => new Vector3(124f, 289f, 21f);

    /// <summary>
    /// マップに置かれた名札つきトリガーポイントを引きます。無ければ null。
    /// </summary>
    public static Vector3? Tagged(string tag)
    {
        if (MapPoints.TryGet(tag, out Vector3 position))
            return position;

        return null;
    }

    /// <summary>
    /// 増援部隊の集合地点です。マップが用意していなければ null を返し、バニラのスポーン地点に任せます。
    /// </summary>
    public static Vector3? FirstTeam => Tagged("FirstTeamSpawnPoint");

    /// <summary>
    /// 部屋のローカル座標をワールド座標にします。部屋がまだ無ければ null。
    /// </summary>
    public static Vector3? InRoom(RoomName name, Vector3 localOffset)
    {
        if (Room.Get(name).FirstOrDefault() is { } room)
            return room.Transform.TransformPoint(localOffset);

        return null;
    }

    /// <summary>
    /// <see cref="RoomName"/> に載っていない部屋をプレハブ名で引きます
    /// (<c>RoomName</c> は 42 種しかなく、EZ_Chef や LCZ_Cafe のような細かい部屋を指せません)。
    /// </summary>
    public static Vector3? InRoom(string prefabName, Vector3 localOffset)
    {
        if (Room.List.FirstOrDefault(room => IsPrefab(room, prefabName)) is { } found)
            return found.Transform.TransformPoint(localOffset);

        return null;
    }

    /// <summary>
    /// 扉のそばを指します。扉がまだ無ければ null。
    /// </summary>
    public static Vector3? AtDoor(DoorName name, Vector3 offset)
    {
        if (Door.Get(name) is { } door)
            return door.Position + offset;

        return null;
    }

    /// <summary>
    /// 部屋の GameObject 名が指定のプレハブ名かどうか。実体は "EZ_Chef(Clone)" のように後ろが付きます。
    /// </summary>
    private static bool IsPrefab(Room room, string prefabName)
    {
        string name = room.GameObject.name;
        int bracket = name.IndexOf('(');

        if (bracket >= 0)
            name = name.Substring(0, bracket);

        return name.Trim().Equals(prefabName, StringComparison.OrdinalIgnoreCase);
    }
}
