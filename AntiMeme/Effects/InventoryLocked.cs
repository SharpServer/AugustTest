using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using RemoteAdmin.Interfaces;

namespace AntiMeme.Effects;

/// <summary>
/// 持ち物を固定します。掛かっている間は拾うことも捨てることもできません。
/// </summary>
/// <remarks>
/// <para>
/// 旧実装の <c>SpecificFlagType.PickingDisabled</c> / <c>DroppingDisabled</c> に当たります。
/// 旧はプレイヤーごとのフラグ集合を別の管理クラスで持っていましたが、
/// 「その人に今掛かっている状態」はエフェクトそのものなので、
/// <see cref="AntiMemeResistance"/> と同じくエフェクト 1 つに畳んであります。
/// </para>
/// <para>
/// <b>弾いたら必ず理由を出します。</b> 黙って弾くと、
/// 操作が効いていないのか仕様なのかプレイヤーには区別が付きません。
/// </para>
/// </remarks>
public class InventoryLocked : CustomEffect, ICustomDisplayName
{
    private const float HintDuration = 3f;

    /// <inheritdoc/>
    public bool CanBeDisplayed => true;

    /// <inheritdoc/>
    public string DisplayName => "Inventory Locked";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Technical;

    /// <inheritdoc/>
    protected override void OnEffectEnabled()
    {
        Hook(
            () => PlayerEvents.PickingUpItem += OnPickingUpItem,
            () => PlayerEvents.PickingUpItem -= OnPickingUpItem);

        Hook(
            () => PlayerEvents.DroppingItem += OnDroppingItem,
            () => PlayerEvents.DroppingItem -= OnDroppingItem);
    }

    private void OnPickingUpItem(PlayerPickingUpItemEventArgs ev)
    {
        if (!IsMine(ev.Player) || !ev.IsAllowed) return;

        ev.IsAllowed = false;
        ev.Player.SendHint("<size=18>あなたはこのアイテムを拾うことができません！</size>", HintDuration);
    }

    private void OnDroppingItem(PlayerDroppingItemEventArgs ev)
    {
        if (!IsMine(ev.Player) || !ev.IsAllowed) return;

        ev.IsAllowed = false;
        ev.Player.SendHint("<size=18>あなたはこのアイテムを捨てることができません！</size>", HintDuration);
    }
}
