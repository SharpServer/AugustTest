using AntiMeme.Items.Bases;
using AntiMeme.Teams.Factions;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// Disarmer Rifle。当てた相手を拘束する単発ライフルです。
///
/// <para>
/// リロードは手動では行えません。撃つと <see cref="CycleSeconds"/> 秒後に自動で持ち替えられ、
/// その 1 秒後に装填されます。実質そこが次弾までの待ち時間になります。
/// </para>
/// </summary>
public sealed class GunDisarmerRifle : CustomWeapon
{
    private const float CycleSeconds = 20f;
    private const float ChamberSeconds = 1f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunE11SR;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.gray;

    /// <inheritdoc/>
    public override string Name => "Disarmer Rifle";

    /// <inheritdoc/>
    public override string Description => "当たった対象を拘束出来るスナイパーライフル";

    /// <inheritdoc/>
    protected override float Damage => 1f;

    /// <inheritdoc/>
    protected override int MagazineSize => 1;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.045f);

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments => [AttachmentName.ScopeSight];

    /// <inheritdoc/>
    protected override bool AllowAttachmentChanges => false;

    /// <inheritdoc/>
    protected override void OnPickedUp(Player player) => Firearm?.Reload();

    /// <inheritdoc/>
    /// <remarks>SCP は拘束できないので素通しします。</remarks>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is null || CustomTeam.Get<ScpTeam>().Includes(ev.Player)) return;

        ev.Player.DisarmedBy = ev.Attacker;
    }

    /// <inheritdoc/>
    protected override void OnShot()
    {
        if (Owner is not { } owner) return;

        // Scope はプレイヤーの退出・ラウンド再開・役職変更で閉じるので、
        // 旧実装のような「20 秒後にまだ生きているか」の確認は要らない。
        PlayerScope.Of(owner).Delay(CycleSeconds, player =>
        {
            if (Item is not { } item) return;

            player.CurrentItem = item;

            PlayerScope.Of(player).Delay(ChamberSeconds, _ => Firearm?.Reload());
        });
    }

    /// <inheritdoc/>
    protected override void OnReloading(PlayerReloadingWeaponEventArgs ev) => ev.IsAllowed = false;
}
