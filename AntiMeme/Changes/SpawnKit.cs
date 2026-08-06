using System.Linq;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;

using ExiledPlayer = Exiled.API.Features.Player;
using ExiledRound = Exiled.API.Features.Round;

namespace AntiMeme.Changes;

/// <summary>
/// 人間側で湧いた全員へ、役職に関係なく配る物です。
/// </summary>
/// <remarks>
/// <para>
/// 平常時は懐中電灯 1 本。エイプリルフールの間は代わりに SCP-330 のキャンディを 1 個配ります。
/// 旧実装では <c>EventHandler.OnChangingRole</c> の中に直接書かれていました。
/// </para>
/// <para>
/// 役職が確定してから配ります。<c>ChangedRole</c> の時点ではバニラの初期装備がまだ入り切って
/// おらず、<see cref="LabApi.Features.Wrappers.Player.IsInventoryFull"/> の判定も
/// 持ち物の確認も当てになりません。旧実装が 0.2 秒待っていたのと同じ理由です。
/// </para>
/// </remarks>
public sealed class SpawnKit : EventHandlerBase
{
    /// <summary>装備が入り切るのを待つ時間です。</summary>
    private const float Delay = 0.2f;

    /// <summary>エイプリルフールに配るキャンディです。</summary>
    private static readonly CandyKindID[] Candies =
    [
        CandyKindID.Black, CandyKindID.Brown, CandyKindID.Gray, CandyKindID.Orange,
        CandyKindID.White, CandyKindID.Evil, CandyKindID.Red, CandyKindID.Blue,
        CandyKindID.Green, CandyKindID.Purple, CandyKindID.Rainbow, CandyKindID.Yellow,
        CandyKindID.Pink,
    ];

    /// <inheritdoc/>
    public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev)
    {
        if (ev.Player is not { IsDestroyed: false } player) return;
        if (ev.NewRole.RoleTypeId is RoleTypeId.Spectator or RoleTypeId.None) return;

        PlayerScope.Of(player).Delay(Delay, owner => Give(owner));
    }

    private static void Give(Player player)
    {
        if (!ExiledRound.InProgress) return;
        if (player is not { IsDestroyed: false, IsAlive: true }) return;

        // SCP は手が塞がっているし、懐中電灯もキャンディも意味がない。
        if (player.IsSCP || player.IsInventoryFull) return;

        if (AntiMemePlugin.Settings.Season is Season.April)
        {
            if (!Has(player, ItemType.SCP330))
                GiveCandy(player);

            return;
        }

        if (!Has(player, ItemType.Flashlight))
            player.AddItem(ItemType.Flashlight);
    }

    private static bool Has(Player player, ItemType type) =>
        player.Items.Any(item => item.Type == type);

    /// <summary>
    /// キャンディ袋への追加はゲーム側の <c>Scp330Bag</c> が持っているので、
    /// そこへ通してくれる EXILED のラッパーを借ります。
    /// </summary>
    private static void GiveCandy(Player player)
    {
        CandyKindID candy = Candies[UnityEngine.Random.Range(0, Candies.Length)];

        if (ExiledPlayer.Get(player.ReferenceHub) is { } exiled)
            exiled.TryAddCandy(candy);
    }
}
