using System.Linq;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// 目の前の扉を素手で破壊します。
/// </summary>
public sealed class AbsolutePowerAbility : AbilityBase
{
    private const float Reach = 3.5f;

    /// <inheritdoc/>
    public override string Name => "驚異的な力";

    /// <inheritdoc/>
    public override string Description => "近くの扉を一撃で破壊する。";

    /// <inheritdoc/>
    public override float Cooldown => 120f;

    /// <inheritdoc/>
    protected override bool CanUse(out string failureReason)
    {
        if (!base.CanUse(out failureReason)) return false;

        if (NearestDoor() is not null) return true;

        failureReason = "近くに破壊できる扉がありません。";

        return false;
    }

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        if (NearestDoor() is { } door)
            door.TryBreak();
    }

    private BreakableDoor NearestDoor()
    {
        Vector3 position = Player.Position;

        return BreakableDoor.List
            .Where(door => !door.IsBroken && Vector3.Distance(door.Position, position) <= Reach)
            .OrderBy(door => Vector3.Distance(door.Position, position))
            .FirstOrDefault();
    }
}
