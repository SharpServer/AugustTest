using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerStatsSystem;
using UnityEngine;

using GameScp3114Role = PlayerRoles.PlayableScps.Scp3114.Scp3114Role;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-3114。SCP-127 の部屋の天井付近から降ってきて、擬態は 5 分保ちます。
/// 擬態の材料になる死体を 2 体その場に置いておきます。
/// </summary>
public class Scp3114 : ScpRole
{
    private const float DisguiseDuration = 300f;

    private static readonly Vector3 SpawnOffset = new Vector3(0f, 13f, 0f);

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 3 1 1 4";

    public override string Name => "SCP-3114";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-3114</color>";

    /// <inheritdoc/>
    public override string Objective => "皆に素敵なサプライズをして驚かせましょう！";

    public override string Description =>
        "人の皮をかぶって擬態し、周りに溶け込む事が出来る骨。\n" +
        "混沌とした施設に絶大な恐怖の一撃を与えよ！";

    public override RoleTypeId BaseRole => RoleTypeId.Scp3114;

    public override float? MaxHealth => 3114f;

    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.Hcz127, SpawnOffset);

    protected override void OnSpawned()
    {
        if (Room.Get(RoomName.Hcz127).FirstOrDefault() is not { } room) return;

        Player.Rotation = room.Rotation;

        // 擬態時間はゲーム側が「擬態が成立した瞬間」に _disguiseDurationSeconds を読んで
        // カウントダウンを開始する。Disguised イベントの中で書き換えても間に合わないので、
        // スポーン時に一度だけ設定しておく。
        if (Player.RoleBase is GameScp3114Role role &&
            role.SubroutineModule.TryGetSubroutine(out Scp3114Identity identity))
        {
            identity._disguiseDurationSeconds = DisguiseDuration;
        }

        SpawnStarterCorpse(RoleTypeId.ClassD, "D-9341", room);
        SpawnStarterCorpse(RoleTypeId.Scientist, "Dr. Maynard", room);
    }

    private void SpawnStarterCorpse(RoleTypeId role, string nickname, Room room)
    {
        Ragdoll.SpawnRagdoll(
            role,
            room.Transform.TransformPoint(SpawnOffset),
            room.Rotation,
            new CustomReasonDamageHandler("For You"),
            nickname);
    }
}
