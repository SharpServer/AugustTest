using Sliced.API.Features;
using PlayerRoles;
using AntiMeme.Roles.Fifthist;
using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles.PlayableScps.Scp096;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// クラス B-記憶処理剤。直近の出来事を忘れる代わりに、頭が揺れます。
/// 忘れられた側 (SCP-096) からもターゲット指定が消えます。
/// </summary>
public sealed class ClassBMemoryRemovePill : CustomUsable, IScp914Upgradable
{
    private const byte ConcussionIntensity = 25;
    private const float ConcussionDuration = 15f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.Adrenaline;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.blue;

    /// <inheritdoc/>
    public override string Name => "クラスB-記憶処理剤";

    /// <inheritdoc/>
    public override string Description => "ここしばらくの出来事や大きな影響を忘却することが出来る。";

    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.ToVanilla(ItemType.SCP500),
        OneToOne = Scp914Rule.To<ClassXMemoryForcePill>(),
        Fine = Scp914Rule.To<SerumC>(),
        VeryFine = Scp914Rule.To<ClassZMemoryForcePill>(),
    };

    /// <inheritdoc/>
    protected override void OnUse(Player player)
    {
        player.EnableEffect<Concussed>(ConcussionIntensity, ConcussionDuration);
        ForgetByScp096(player);
        RestoreFifthistConversion(player);
    }

    /// <summary>
    /// 第五教会に改宗させられていたなら、改宗前のバニラ役職へ戻します。
    /// 改宗前が記録されていない場合は D クラスとして扱います。
    /// </summary>
    private static void RestoreFifthistConversion(Player player)
    {
        if (CustomRole.Of(player) is not FifthistConvert convert) return;

        RoleTypeId previous = convert.PreviousRole;

        if (previous is RoleTypeId.None or RoleTypeId.Destroyed or RoleTypeId.Spectator or RoleTypeId.Overwatch)
            previous = RoleTypeId.ClassD;

        CustomRole.Remove(player);
        player.SetRole(previous, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None);
    }

    /// <summary>
    /// SCP-096 のターゲット指定を外します。「顔を見たこと」ごと忘れさせる、という扱いです。
    /// </summary>
    private static void ForgetByScp096(Player forgotten)
    {
        foreach (Player other in Player.ReadyList)
        {
            if (other.RoleBase is not Scp096Role scp096) continue;
            if (!scp096.SubroutineModule.TryGetSubroutine(out Scp096TargetsTracker tracker)) continue;

            tracker.RemoveTarget(forgotten.ReferenceHub);
        }
    }
}
