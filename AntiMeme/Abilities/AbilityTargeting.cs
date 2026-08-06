using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// 範囲系アビリティが共有する「誰に当てるか」の判定です。
///
/// <para>
/// 旧実装はどのアビリティも <c>Player.List</c> を自前で回し、<c>GetTeam() != CTeam.Fifthists</c> のような
/// 具体的な陣営名を直書きしていました。ここでは <see cref="CustomTeam"/> に聞くだけにしてあるので、
/// チーム層 (担当 10) が陣営を宣言した時点で全アビリティの敵味方判定がそのまま正しくなります。
/// </para>
/// <para>
/// チームがまだ 1 つも宣言されていない段階ではバニラの <see cref="LabApi.Features.Wrappers.Player.Faction"/> に落ちます。
/// </para>
/// </summary>
internal static class AbilityTargeting
{
    /// <summary>
    /// 指定した中心から半径内にいる、自分以外の生存プレイヤーです。
    /// </summary>
    public static IEnumerable<Player> Around(Player owner, Vector3 center, float radius)
    {
        float radiusSqr = radius * radius;

        return Player.ReadyList.Where(other =>
            !ReferenceEquals(other, owner) &&
            other.IsAlive &&
            (other.Position - center).sqrMagnitude <= radiusSqr);
    }

    /// <summary>
    /// 自分の周囲半径内にいる、自分以外の生存プレイヤーです。
    /// </summary>
    public static IEnumerable<Player> Around(Player owner, float radius) => Around(owner, owner.Position, radius);

    /// <summary>
    /// 指定した中心から半径内にいる敵です。
    /// </summary>
    public static IEnumerable<Player> EnemiesAround(Player owner, Vector3 center, float radius) =>
        Around(owner, center, radius).Where(other => !IsAlly(owner, other));

    /// <summary>
    /// 自分の周囲半径内にいる敵です。
    /// </summary>
    public static IEnumerable<Player> EnemiesAround(Player owner, float radius) =>
        EnemiesAround(owner, owner.Position, radius);

    /// <summary>
    /// このプレイヤーが属しているチームです。どこにも属していなければ null。
    /// </summary>
    public static CustomTeam TeamOf(Player player) => CustomTeam.Of(player);

    /// <summary>
    /// 味方かどうか。両者ともチームに属していればチームで、そうでなければバニラの陣営で判定します。
    /// </summary>
    public static bool IsAlly(Player self, Player other) => CustomTeam.AreAllies(self, other);
}
