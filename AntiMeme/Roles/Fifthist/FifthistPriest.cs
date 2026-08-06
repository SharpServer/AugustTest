using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using AntiMeme.Maps;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Fifthist;

public sealed class FifthistPriest : FifthistRole
{
    private const float AuraRadius = 2.75f;
    private const float AuraInterval = 1.5f;
    private const float AuraDamage = 25f;

    public override string Name => "第五教会 司祭";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff00fa>Fifthist: Priest</color>";

    /// <inheritdoc/>
    public override string Objective => "あなたは幸福な事に第五の加護を受けている。全てを第五せよ！";
    public override string Description => "非常に<color=#ff00fa>第五的</color>な存在の恩寵を受けた第五主義者。\n施設を占領せよ！";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override Vector3? SpawnPosition => SpawnPoints.ExitB;
    public override Vector3? Scale => Vector3.one * 1.1f;
    public override float? MaxHealth => 555f;
    public override string CustomInfo => "<color=#FF0090>Fifthist Priest</color>";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.GunCOM18, ItemType.ArmorHeavy, ItemType.SCP500, ItemType.Adrenaline, ItemType.GrenadeHE ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(KeycardFifthistPriest), typeof(LsdPillSafe) ];

    protected override void OnSpawned()
    {
        RoleWear wear = RoleWear.AttachSchematic(Player, "SCP3005", scale: Vector3.zero);
        if (wear != null)
        {
            wear.WithLight(Color.magenta, offset: new Vector3(0f, -0.08f, 0f));
            Scope.OnDispose(_ => wear.Dispose());
        }

        Scope.RunLoop(AuraInterval, _ => BurnNearby());
    }

    private void BurnNearby()
    {
        foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
        {
            if (IsMine(target) || !target.IsAlive || Team.Includes(target))
                continue;

            if ((target.Position - Player.Position).sqrMagnitude > AuraRadius * AuraRadius)
                continue;

            target.Damage(AuraDamage, Player);
            Player.SendHitMarker();
        }
    }
}
