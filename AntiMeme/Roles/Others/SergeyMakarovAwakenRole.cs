using System;
using System.Collections.Generic;
using AntiMeme.Abilities;
using AntiMeme.Items.Melee;
using AntiMeme.Maps;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MEC;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Others;

public sealed class SergeyMakarovAwakenRole : OtherRole
{
    public override string Name => "呪詛 - セルゲイ・マカロフ";

    /// <inheritdoc/>
    public override string HudLabel => "<color=red>Cursemaster - Sergey Makarov</color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#a0a0a0>Alone</color>";

    /// <inheritdoc/>
    public override string Objective => "<color=red><b>邪魔者を滅ぼし、サイト-02から毒を浄化せよ</b></color>";
    public override string Description => "<size=25>怨念に呑まれ、全てを排除せんと暴れ狂う嘗ての管理官。\nアビリティ「怨みの沼, 呪詛, 管理官の祟り」が使用可能だ。\n<color=red><b>邪魔者を滅ぼし、サイト-02から毒を浄化せよ。</b></color>";
    public override RoleTypeId BaseRole => RoleTypeId.Scp0492;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.Hcz106, Vector3.up * 0.25f);
    public override float? MaxHealth => 5000f;
    public override string CustomInfo => "SPIRIT OF CURSEMASTER";

    /// <summary>
    /// 能力そのものは普通のものと同じで、<b>呼び名だけが怨霊のものになります</b>。
    /// 呼び名を決めているのは能力ではなくこの役職なので、配るここで名札を貼り替えます
    /// (能力側に「持ち主がセルゲイか」を判定させない)。
    /// </summary>
    protected override void OnSpawned()
    {
        AbilityBase.Give<CreateSinkholeAbility>(Player)?.Rename("怨みの沼");
        AbilityBase.Give<MindblasterAbility>(Player)?.Rename("呪詛");
        AbilityBase.Give<SoundOfFifthAbility>(Player)?.Rename("管理官の祟り");
    }
}
