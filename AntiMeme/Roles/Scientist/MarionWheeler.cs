using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Scientist;

public sealed class MarionWheeler : ScientistRole
{
    public override string Name => "マリオン・ホイーラー";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ffa500>Marion Wheeler</color>";

    /// <inheritdoc/>
    public override string Objective => "第五の目を搔い潜り、反ミーム爆弾を起爆しろ！";
    public override string Description => "現在貴方の部門は壊滅状態に陥っている...\n下層のDクラス収容房最奥にある反ミーム爆弾を起動してこのアウトブレイクをリセットしなければならない。\n<color=red>例え命を落とそうとも</color>";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom("EZ_Intercom", Vector3.up * 1.25f);
    public override float? MaxHealth => 120f;
    public override string CustomInfo => "Marion Wheeler";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.KeycardContainmentEngineer, ItemType.Medkit, ItemType.Medkit,
      ItemType.Medkit ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(GunScp7381), typeof(ClassZMemoryForcePill) ];
}
