using AntiMeme.Roles.Scps;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp106;
using Sliced.API.Features;

namespace AntiMeme.Abilities;

/// <summary>
/// SCP-106 が追跡対象のいる部屋へポータルで移動します。
///
/// <para>
/// 追跡対象は <see cref="Scp106.HuntTarget"/> が持っています。旧実装は
/// <c>Scp106Role.TryGetHuntTarget(player, out target)</c> という静的辞書引きでしたが、
/// 役職側がプレイヤー 1 人 = 1 インスタンスになったのでプロパティを見るだけで済みます。
/// </para>
/// </summary>
public sealed class Scp106TargetTeleportAbility : AbilityBase
{
    private const float VigorCost = 0f;

    /// <inheritdoc/>
    public override string Name => "獲物への侵食";

    /// <inheritdoc/>
    public override string Description => "追跡対象のいる部屋へ潜って移動する。";

    /// <inheritdoc/>
    public override float Cooldown => 20f;

    /// <inheritdoc/>
    protected override bool CanUse(out string failureReason)
    {
        if (!base.CanUse(out failureReason)) return false;

        if (Player.RoleBase is not Scp106Role role ||
            !role.SubroutineModule.TryGetSubroutine(out Scp106HuntersAtlasAbility atlas))
        {
            failureReason = "SCP-106でなければ使用できません。";

            return false;
        }

        if (atlas._syncSubmerged || role.Sinkhole.IsDuringAnimation)
        {
            failureReason = "現在はポータルを使用できません。";

            return false;
        }

        if (HuntTarget is not { } target)
        {
            failureReason = "追跡対象が存在しません。";

            return false;
        }

        if (!IsReachable(target))
        {
            failureReason = "対象のいる場所にはテレポートできません。";

            return false;
        }

        if (role.SubroutineModule.TryGetSubroutine(out Scp106VigorAbilityBase vigor) && vigor.VigorAmount < VigorCost)
        {
            failureReason = "Vigorが不足しています。";

            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        if (Player.RoleBase is not Scp106Role role ||
            !role.SubroutineModule.TryGetSubroutine(out Scp106HuntersAtlasAbility atlas) ||
            HuntTarget is not { } target ||
            target.Room is not { } room)
            return;

        atlas._syncRoom = room.Base;
        atlas._syncPos = target.Position;
        atlas._estimatedCost = VigorCost;
        atlas._syncSubmerged = true;

        Player.SendHint($"<color=#c50000>{target.Nickname}</color>のいる部屋へ移動を開始します。", 4f);
    }

    private Player HuntTarget => CustomRole.Of(Player) is Scp106 hunter ? hunter.HuntTarget : null;

    /// <summary>
    /// ポケットや地上へは潜れません。部屋の外に居る相手も追えません。
    /// </summary>
    private static bool IsReachable(Player target) =>
        target is { IsDestroyed: false, IsAlive: true } &&
        target.Room is { } room &&
        room.Name is not (RoomName.Outside or RoomName.Pocket) &&
        room.Zone is not (FacilityZone.Surface or FacilityZone.Other or FacilityZone.None);
}
