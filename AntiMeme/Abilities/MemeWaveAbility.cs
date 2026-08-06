using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles.PlayableScps.Scp079;
using Sliced.API.Features;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Abilities;

/// <summary>敵対するアラ・オルンのアクセス Tier を 1 段階下げます。</summary>
public sealed class MemeWaveAbility : AbilityBase
{
    public override string Name => "ミーム波動";

    public override string Description => "稼働中のアラ・オルンへ干渉し、アクセス Tier を低下させます。";

    public override float Cooldown => 150f;

    protected override bool CanUse(out string failureReason)
    {
        if (!base.CanUse(out failureReason)) return false;

        if (!Targets().Any())
        {
            failureReason = "干渉可能な対象が存在しません。";
            return false;
        }

        failureReason = null;
        return true;
    }

    protected override void OnUsed()
    {
        foreach (Scp079TierManager tiers in Targets())
        {
            int currentIndex = tiers.AccessTierIndex;
            if (currentIndex <= 0) continue;

            // AbsoluteThresholds[i] は AccessTierIndex i+1 の下限値です。
            tiers.TotalExp = currentIndex == 1
                ? 0
                : tiers.AbsoluteThresholds[currentIndex - 2];
        }
    }

    private System.Collections.Generic.IEnumerable<Scp079TierManager> Targets()
    {
        foreach (Player target in Player.ReadyList)
        {
            if (target == Player || !target.IsAlive || CustomRole.Of(target) is not AraOrun)
                continue;

            if (target.RoleBase is Scp079Role role &&
                role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tiers) &&
                tiers.AccessTierIndex > 0)
            {
                yield return tiers;
            }
        }
    }
}
