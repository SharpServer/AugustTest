using System.Linq;
using AntiMeme.Items;
using AntiMeme.Maps.Features;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>SCP-513 のベルを鳴らし、所持者を対象へ登録するアイテムです。</summary>
public sealed class Scp513Item : CustomItem
{
    private static bool hooked;

    public Scp513Item() => Hook();

    public override ItemType BaseType => ItemType.Coin;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.gray;
    public override string Name => "SCP-513";
    public override string Description => "???";

    /// <inheritdoc/>
    protected override string PickupModel => "SCP513ItemModel";

    private void Flip(PlayerFlippingCoinEventArgs ev)
    {
        if (ev.CoinItem.Serial != Serial || ev.Player.CurrentItem?.Serial != Serial) return;

        ev.IsAllowed = false;
        Scp513.AddTarget(ev.Player);
        ev.Player.SendHint("<size=25>何か視線を感じる気がする...</size>", 3f);

        Room room = Room.List
            .Where(candidate => candidate.Zone == FacilityZone.HeavyContainment)
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();
        if (room is not null)
            CustomItem.Spawn<Scp513Item>(room.Position + Vector3.up * .25f);

        Destroy();
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.FlippingCoin += OnFlippingCoin;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.FlippingCoin -= OnFlippingCoin;
            hooked = false;
        });
    }

    private static void OnFlippingCoin(PlayerFlippingCoinEventArgs ev) =>
        (Of(ev.CoinItem.Serial) as Scp513Item)?.Flip(ev);
}
