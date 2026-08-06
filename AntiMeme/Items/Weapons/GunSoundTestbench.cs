using AntiMeme.Audio;
using AntiMeme.Items.Weapons;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>Debug firearm that records the selected weapon's server sound metadata without replacing audio.</summary>
public sealed class GunSoundTestbench : HybridWeapon
{
    public override ItemType BaseType => ItemType.GunCOM15;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.yellow;
    public override string Name => "Gun Sound Testbench";
    public override string Description => "音声層の採取用。発砲・空撃ち・リロードを行うと AudioIndex と種類をログへ出します。";
    protected override System.Type NextMode => typeof(GunSoundTestbench);

    protected override void OnEquipped(Player player)
    {
        base.OnEquipped(player);
        Logger.Info($"[GunSoundTestbench] equipped by {player?.Nickname ?? "unknown"}; firearm sound capture is active.");
    }

    protected override void OnCreated() => GunSoundResolver.SetCapture(Serial, true);

    protected override void OnReleased() => GunSoundResolver.SetCapture(Serial, false);
}
