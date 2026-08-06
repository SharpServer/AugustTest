using AntiMeme.Items.Bases;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Events.Arguments.PlayerEvents;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// XE-11 ANOMALY DETAINER。SCP に当てると強い鈍足を付ける対異常兵装です。
/// </summary>
public sealed class GunAnomalyDetainer : CustomWeapon
{
    private const byte SlownessIntensity = 35;
    private const float SlownessDuration = 7f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GunE11SR;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.35f, 0.8f, 1f);

    /// <inheritdoc/>
    public override string Name => "XE-11 ANOMALY DETAINER";

    /// <inheritdoc/>
    public override string Description => "被弾したSCPに強力な鈍足を付与する試作対異常兵装";

    /// <inheritdoc/>
    protected override float Damage => 20f;

    /// <inheritdoc/>
    protected override int MagazineSize => 5;

    /// <inheritdoc/>
    protected override Vector3 Scale => new Vector3(1f, 1f, 1.1f);

    /// <inheritdoc/>
    protected override AttachmentName[] Attachments =>
    [
        AttachmentName.RifleBody,
        AttachmentName.NightVisionSight,
        AttachmentName.StandardStock,
        AttachmentName.Foregrip,
        AttachmentName.LowcapMagJHP,
        AttachmentName.FlashHider,
    ];

    /// <inheritdoc/>
    protected override bool AllowAttachmentChanges => false;

    /// <inheritdoc/>
    /// <remarks>
    /// 旧実装は <c>IsScp || GetTeam() == CTeam.SCPs</c> と 2 つ書いていました。
    /// チームが所属判定を自分で持つので、いまは 1 回訊くだけで済みます。
    /// </remarks>
    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (!CustomTeam.Get<ScpTeam>().Includes(ev.Player)) return;

        ev.Player.EnableEffect<Slowness>(SlownessIntensity, SlownessDuration);
    }
}
