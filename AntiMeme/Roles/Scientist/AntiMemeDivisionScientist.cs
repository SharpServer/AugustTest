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

public sealed class AntiMemeDivisionScientist : ScientistRole
{
    public override string Name => "反ミーム部門職員";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#faff86>Anti-Meme Division Scientist</color>";

    /// <inheritdoc/>
    public override string Objective => "反ミーム爆弾を起動し、第五教会によるアウトブレイクをリセットせよ。たとえ命を落としても。";
    public override string Description => "現在貴方の部門は壊滅状態に陥っている...\n下層のDクラス収容房最奥にある反ミーム爆弾を起動してこのアウトブレイクをリセットしなければならない。\n<color=red>例え命を落とそうとも</color>";
    public override RoleTypeId BaseRole => RoleTypeId.Scientist;
    public override Vector3? SpawnPosition => SpawnPoints.InRoom("EZ_Intercom", Vector3.up * 1.25f);
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Anti Memetic Division Scientist";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCOM15 ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(KeycardHeadResearcher), typeof(ClassZMemoryForcePill) ];
}
