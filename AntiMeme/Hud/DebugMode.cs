using System.Collections.Generic;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Hud;

/// <summary>
/// デバッグ HUD の on/off と、そこに出す「最後に触ったドア」を覚えておきます。
/// </summary>
/// <remarks>
/// 旧 <c>DebugModeHandler</c> と同じで、<b>本人が明示的に入れたときだけ</b>出します。
/// オーバーウォッチかどうかでは判定しません (観戦しているだけの運営に出しても邪魔なだけです)。
/// プレイヤーごとの状態は <see cref="PlayerScope"/> に載っているので、
/// 退出とラウンド再開で勝手に消えます。
/// </remarks>
public static class DebugMode
{
    private static readonly HashSet<Player> Enabled = new HashSet<Player>();
    private static readonly Dictionary<Player, DoorSnapshot> LastDoors = new Dictionary<Player, DoorSnapshot>();

    /// <summary>最後に触ったドアの、マップ作成時に見たい値だけを写したものです。</summary>
    public readonly struct DoorSnapshot
    {
        public DoorSnapshot(string name, string room, Vector3 localPosition, Vector3 localEuler, Vector3 roomEuler)
        {
            Name = name;
            Room = room;
            LocalPosition = localPosition;
            LocalEuler = localEuler;
            RoomEuler = roomEuler;
        }

        public string Name { get; }
        public string Room { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalEuler { get; }
        public Vector3 RoomEuler { get; }
    }

    /// <summary>このプレイヤーがデバッグ HUD を出しているかどうか。</summary>
    public static bool IsOn(Player player) => player is { IsDestroyed: false } && Enabled.Contains(player);

    /// <summary>デバッグ HUD を切り替えます。切り替えた後の状態を返します。</summary>
    public static bool Toggle(Player player)
    {
        if (IsOn(player))
        {
            Enabled.Remove(player);

            return false;
        }

        Track(player);
        Enabled.Add(player);

        return true;
    }

    /// <summary>最後に触ったドアを覚えます。</summary>
    public static void Remember(Player player, DoorSnapshot door)
    {
        if (player is not { IsDestroyed: false }) return;

        Track(player);
        LastDoors[player] = door;
    }

    /// <summary>覚えているドアを引きます。</summary>
    public static bool TryGetDoor(Player player, out DoorSnapshot door) =>
        LastDoors.TryGetValue(player, out door);

    private static void Track(Player player)
    {
        if (Enabled.Contains(player) || LastDoors.ContainsKey(player)) return;

        PlayerScope.Of(player).OnDispose(owner =>
        {
            Enabled.Remove(owner);
            LastDoors.Remove(owner);
        });
    }
}
