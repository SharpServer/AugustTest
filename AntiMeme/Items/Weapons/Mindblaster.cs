using AntiMeme.Abilities;
using AntiMeme.Items.Bases;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// 第五思壊線。投擲操作で第五的な光を撃ち出すカードです。
///
/// <para>
/// 撃つと <see cref="ChargeSeconds"/> 秒のチャージに入り、その間は見た目が
/// 「チャージ中」に変わります。弾体の飛翔と命中判定は
/// <see cref="MindblasterAbility"/> が持っています。
/// </para>
/// </summary>
/// <remarks>
/// 旧実装は「発射可能カード」と「チャージ中カード」を<b>別アイテムとして持ち替える</b>作りで、
/// そのために 411 行のうち大半がシリアルの張り替え
/// (<c>RuntimeState</c> 辞書 / <c>TryChangeInventoryMode</c> / <c>_pendingRuntimeState</c> /
/// <c>SerialTracker.ForceUnregister</c>) に費やされていました。
/// アイテム 1 個 = 1 インスタンスなので、状態はただのフィールドで足り、
/// 見た目は <see cref="CustomKeycard.Refresh"/> で焼き直すだけで済みます。
/// </remarks>
public sealed class Mindblaster : CustomKeycard
{
    /// <summary>発射回数が無制限であることを表す値です。</summary>
    public const int InfiniteShots = -1;

    private const float ChargeSeconds = 10f;

    private bool charging;
    private float chargedAt;

    /// <summary>
    /// 残りの発射回数です。<see cref="InfiniteShots"/> なら無制限。
    /// </summary>
    public int RemainingShots { get; set; } = InfiniteShots;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomTaskForce;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.magenta;

    /// <inheritdoc/>
    public override string Name => charging ? "第五思壊線（チャージ中）" : "第五思壊線";

    /// <inheritdoc/>
    public override string Description =>
        charging ? "第五思壊線をチャージしている" : "非常に第五的な光を発射し思考を破壊する";

    /// <inheritdoc/>
    protected override string Label => charging ? "CHARGING" : "Mindblaster";

    /// <inheritdoc/>
    protected override Color32 LabelColor =>
        charging ? new Color32(210, 210, 210, 255) : new Color32(255, 0, 250, 255);

    /// <inheritdoc/>
    protected override Color32 Tint =>
        charging ? new Color32(105, 105, 105, 255) : new Color32(255, 0, 250, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor =>
        charging ? new Color32(145, 145, 145, 255) : new Color32(255, 255, 255, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Mgc. Fifth";

    /// <inheritdoc/>
    protected override string SerialNumber => charging ? "CHARGING" : "555555555555";

    /// <inheritdoc/>
    protected override int Rank => 1;

    private float RemainingCharge => Mathf.Max(0f, chargedAt - Time.time);

    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        // 投げる操作を発射に使う。カードそのものは手元に残す。
        ev.IsAllowed = false;

        Player player = ev.Player;

        if (charging)
        {
            player.SendHint($"<size=23>チャージ中です（残り {RemainingCharge:0.#}秒）。</size>", 2f);

            return;
        }

        if (RemainingShots == 0)
        {
            player.SendHint("<size=23>第五思壊線の発射回数を使い切っています。</size>", 3f);

            return;
        }

        if (!AbilityBase.Give<MindblasterAbility>(player).TryUse(out string failureReason))
        {
            player.SendHint($"<size=23>{failureReason}</size>", 3f);

            return;
        }

        if (RemainingShots > 0)
            RemainingShots--;

        if (RemainingShots == 0)
        {
            Destroy();
            player.SendHint("<size=23>第五思壊線の発射回数を使い切りました。</size>", 3f);

            return;
        }

        StartCharge(player);
    }

    private void StartCharge(Player player)
    {
        charging = true;
        chargedAt = Time.time + ChargeSeconds;
        Refresh();

        player.SendHint($"<size=23>第五思壊線をチャージ中です（{ChargeSeconds:0.#}秒）。</size>", 3f);

        // 持ち主が居なくなればスコープごと消えるので、後片付けは要らない。
        PlayerScope.Of(player).Delay(ChargeSeconds, owner =>
        {
            charging = false;
            Refresh();

            owner.SendHint($"<size=23>第五思壊線のチャージが完了しました。\n{ShotsText}</size>", 3f);
        });
    }

    private string ShotsText =>
        RemainingShots < 0 ? "残り発射回数: ∞" : $"残り発射回数: {RemainingShots}";

}
