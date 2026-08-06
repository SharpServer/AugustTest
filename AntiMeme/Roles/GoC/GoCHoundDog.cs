using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Nvg;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Structs;
using AntiMeme.Roles.FoundationForces;

namespace AntiMeme.Roles.GoC;

public sealed class GoCHoundDog : GoCRole
{
    private bool previousBypass;

    public override string Name => "GoC: Hound Dog マークⅡ戦闘強化服Combat Garment(ホワイト・スーツ)";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#0000c8>Hound Dog: White Suit</color>";
    public override string Description => "GoC製のとても強い戦闘強化服。色んな機能・装備が盛り込まれている。\nPassive: ホワイトスーツ\nホワイトスーツの超駆動により常時コーラ一本分の速度を提供する。\nPassive: VERITAS\n遠くにいる敵等を認識できる\nPassive: 自爆装置\n死亡、拘束され際に起動し、グレネード一個分の自爆を引き起こす。\nPassive: 自動認証\n施設内のすべてのキーカード認証等を素通りできる。";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSpecialist;
    public override float? MaxHealth => 120f;
    public override string CustomInfo => "Global Occult Collision: Hound Dog Mark II Combat Garment White Suit";
    public override IReadOnlyList<ItemType> Items =>
    [ ItemType.Adrenaline, ItemType.Medkit ];
    public override IReadOnlyList<Type> CustomItems =>
    [ typeof(ArmorVip), typeof(GunSuperLogicer), typeof(GunGoCRailgunFull), typeof(CloakGenerator) ];
    public override IReadOnlyDictionary<ItemType, ushort> Ammo => new Dictionary<ItemType, ushort>
    { [ItemType.Ammo762x39] = 140 };
    public override IReadOnlyList<RoleEffect> Effects =>
    [ RoleEffect.Of<MovementBoost>(25), RoleEffect.Of<Scp1344>() ];

    protected override void OnSpawned()
    {
        previousBypass = Player.IsBypassEnabled;
        Player.IsBypassEnabled = true;
        SetHumeShield(1500f);
        BoostHumeShieldRegen(3.5f);

        Hook(() => PlayerEvents.Dying += OnDying, () => PlayerEvents.Dying -= OnDying);
        Hook(() => PlayerEvents.Cuffing += OnCuffing, () => PlayerEvents.Cuffing -= OnCuffing);
    }

    protected override void OnRemoved()
    {
        if (Player is { IsDestroyed: false })
            Player.IsBypassEnabled = previousBypass;
    }

    private void OnDying(PlayerDyingEventArgs ev)
    {
        if (IsMine(ev.Player))
            TimedGrenadeProjectile.SpawnActive(ev.Player.Position, ItemType.GrenadeHE, ev.Player, 0.01);
    }

    private void OnCuffing(PlayerCuffingEventArgs ev)
    {
        if (!IsMine(ev.Target)) return;

        ev.IsAllowed = false;
        ev.Target.Kill("自爆機能による力");
        if (TimedGrenadeProjectile.SpawnActive(ev.Target.Position, ItemType.GrenadeFlash, ev.Target, 0.01)
            is FlashbangProjectile flash)
        {
            flash.BaseBlindTime += 5.5f;
        }
    }
}
