using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// Zombified Subject。SCP-049 以外の要因でゾンビ化した人間です。
/// その場でゾンビになるのが基本で、そもそも部屋の中にいなかった場合だけ HCZ のどこかへ出します。
/// </summary>
public class Zombified : ScpRole
{
    public override string Name => "Zombified Subject";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>Zombified Subject</color>";

    /// <inheritdoc/>
    public override string Objective => "何らかの要因でゾンビの様になってしまった。とにかく暴れろ！";

    public override string Description =>
        "様々な要因によりゾンビと化してしまった人の成れの果て。\n" +
        "暴れまくって施設に混沌をもたらせよ！";

    public override RoleTypeId BaseRole => RoleTypeId.Scp0492;

    public override float? MaxHealth => 125f;

    public override string CustomInfo => "<color=#C50000>Zombified Subject</color>";

    /// <summary>
    /// ゾンビ化は「今いる場所で起きる」ものなので、その場から少しだけ持ち上げます。
    /// 床にめり込んだ状態でロールが切り替わると落下判定に巻き込まれるためです。
    /// </summary>
    public override Vector3? SpawnPosition =>
        Player.Room is not null
            ? Player.Position + Vector3.up * 0.85f
            : RandomHeavyContainmentPoint();

    private static Vector3? RandomHeavyContainmentPoint()
    {
        Room[] rooms = Room.List
            .Where(room => room.Zone == FacilityZone.HeavyContainment)
            .ToArray();

        return rooms.Length == 0
            ? null
            : rooms[Random.Range(0, rooms.Length)].Transform.TransformPoint(Vector3.up * 1.05f);
    }
}
