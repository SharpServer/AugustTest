using System.Linq;
using AntiMeme.Abilities;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using Sliced.API.Features;
using UnityEngine;

using GameScp079Role = PlayerRoles.PlayableScps.Scp079.Scp079Role;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-079。最初から Tier 5 で、エネルギー上限と SCP-2176 のロックアウト時間だけ書き換えます。
///
/// 数値はすべてゲーム本体のサブルーチン (<see cref="Scp079TierManager"/> /
/// <see cref="Scp079AuxManager"/> / <see cref="Scp079LostSignalHandler"/>) に直接書きます。
/// EXILED のラッパー越しでも結局この 3 つを触るので、1 段減らしています。
/// </summary>
public class Scp079 : ScpRole
{
    private const int StartingTier = 5;
    private const float MaxEnergy = 1000f;
    private const float GhostlightLockout = 5f;
    private bool overrideScheduled;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 0 7 9";

    public override string Name => "SCP-079";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-079</color>";

    /// <inheritdoc/>
    public override string Objective => "施設制御システムを操り、施設に混沌を引き起こせ。";

    public override string Description =>
        "施設制御システムを操り、施設に混沌を引き起こす。\n" +
        "巧みに立ち回り自身の目的を達成せよ！";

    public override RoleTypeId BaseRole => RoleTypeId.Scp079;

    public override string CustomInfo => "SCP-079";

    protected override void OnSpawned()
    {
        overrideScheduled = false;

        if (Player.RoleBase is not GameScp079Role role) return;

        if (role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tiers))
        {
            // AbsoluteThresholds[n] は「Tier n+2 に到達する経験値」。Tier 5 なら添字 3。
            int[] thresholds = tiers.AbsoluteThresholds;
            tiers.TotalExp = thresholds[Mathf.Clamp(StartingTier - 2, 0, thresholds.Length - 1)];
        }

        if (role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager aux))
        {
            // MaxAux は現在の Tier のエントリを読むだけなので、そこを書き換える。
            aux._maxPerTier[aux._tierManager.AccessTierIndex] = MaxEnergy;
        }

        if (role.SubroutineModule.TryGetSubroutine(out Scp079LostSignalHandler lostSignal))
            lostSignal._ghostlightLockoutDuration = GhostlightLockout;

        Hook(
            () => ServerEvents.GeneratorActivated += OnGeneratorActivated,
            () => ServerEvents.GeneratorActivated -= OnGeneratorActivated);
    }

    /// <summary>
    /// 発電機が全て起動したことを本人に伝えます。
    ///
    /// <c>PlayerEvents.ActivatedGenerator</c> は起動レバーを倒した時点で飛ぶので、
    /// 実際に Engaged になる <c>ServerEvents.GeneratorActivated</c> を見ます
    /// (旧実装が 1 秒の遅延を挟んでいたのはこれを避けるためでした)。
    ///
    /// 旧実装はここから 60 秒後に ALPHA WARHEAD OVERRIDE アビリティを配っていましたが、
    /// アビリティ層は別担当なので警告だけを残しています。
    /// </summary>
    private void OnGeneratorActivated(GeneratorActivatedEventArgs ev)
    {
        if (Generator.List.Any(generator => !generator.Engaged) || overrideScheduled) return;

        overrideScheduled = true;

        Player.SendHint(
            "<size=23><color=red>!!!!!発電機が全て起動されました!!!!!\n最終手段を確立しています・・・</color></size>",
            8f);

        Scope.Delay(60f, owner =>
        {
            if (!CustomRole.Is<Scp079>(owner) || Generator.List.Any(generator => !generator.Engaged))
                return;

            AbilityBase.Give<AlphaWarheadOverrideAbility>(owner);
            owner.SendHint(
                "<color=red><b>ALPHA WARHEAD OVERRIDEが使用可能になりました！</b></color>\n" +
                "アビリティ使用キーを押して施設を破壊しましょう！",
                8f);
        });
    }
}
