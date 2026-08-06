using Exiled.API.Enums;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// 目の前の床に一時的な陥没を作ります。
/// </summary>
public sealed class CreateSinkholeAbility : AbilityBase
{
    private const float Lifetime = 10f;
    private const float Reach = 12f;
    private const float GroundProbe = 10f;
    private const float FloorOffset = 0.01f;

    /// <inheritdoc/>
    public override string Name => "シンクホール";

    /// <inheritdoc/>
    public override string Description => "見ている床を陥没させ、踏んだ相手の足を止める。";

    /// <inheritdoc/>
    public override float Cooldown => 60f;

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        SinkholeHazard sinkhole = SinkholeHazard.Spawn(AimedGround(Player, Reach, GroundProbe, FloorOffset), Quaternion.identity, Vector3.one);

        RoundScope.Current.Delay(Lifetime, sinkhole.Destroy);
    }

    /// <summary>
    /// 視線の先にある床の座標です。壁に当たった場合はその足元まで降ろします。
    /// 何にも当たらなければ目の前へ置きます。
    /// </summary>
    internal static Vector3 AimedGround(Player player, float reach, float probe, float offset)
    {
        Transform camera = player.Camera;
        int worldCollision = (int)LayerMasks.OnlyWorldCollision;

        if (!Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, reach, worldCollision))
            return player.Position + camera.forward * 3.5f;

        return Physics.Raycast(hit.point + Vector3.up * 3f, Vector3.down, out RaycastHit ground, probe, worldCollision)
            ? ground.point + Vector3.up * offset
            : hit.point + Vector3.up * offset;
    }
}
