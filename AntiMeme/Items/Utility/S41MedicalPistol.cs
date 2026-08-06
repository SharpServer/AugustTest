using AntiMeme.Items.Bases;
using AntiMeme.Items.Weapons;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using InventorySystem.Items.Firearms.Attachments;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>
/// S-41 Medical Pistol。人間に当てると治療し、SCP には何もしない医療銃です。
/// </summary>
public sealed class S41MedicalPistol : CustomWeapon
{
    private const int MedicalPistolMagazineSize = 6;
    private const byte HealIntensity = 1;
    private const float HealDuration = 5f;

    public override ItemType BaseType => ItemType.GunCOM18;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0.35f, 1f, 0.65f);

    public override string Name => "S-41 MEDICAL PISTOL";

    public override string Description =>
        "着弾した人間に鎮痛剤のようなリジェネ回復を付与し、火傷・出血・窒息・心停止を治療する。SCPには効果がない。";

    protected override int MagazineSize => MedicalPistolMagazineSize;

    protected override AttachmentName[] Attachments =>
    [
        AttachmentName.StandardMagFMJ,
        AttachmentName.SoundSuppressor,
    ];

    protected override bool AllowAttachmentChanges => false;

    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is not { } attacker || CustomTeam.Get<ScpTeam>().Includes(ev.Player))
            return;

        // This is a healing shot, never damage the target.
        ev.IsAllowed = false;
        Treat(ev.Player);
        attacker.SendHitMarker(1.5f);
    }

    private static void Treat(Player player)
    {
        player.DisableEffect<Burned>();
        player.DisableEffect<Bleeding>();
        player.DisableEffect<Hemorrhage>();
        player.DisableEffect<Asphyxiated>();
        player.DisableEffect<CardiacArrest>();
        player.EnableEffect<Invigorated>(HealIntensity, HealDuration);
        Regeneration.Apply(player, 1f, HealDuration);

        if (player.Health < player.MaxHealth)
            player.Health = UnityEngine.Mathf.Min(player.MaxHealth, player.Health + 5f);
    }
}
