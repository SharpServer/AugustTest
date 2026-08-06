using InventorySystem.Items.Usables;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Items.Bases;

/// <summary>
/// 使い切り型カスタムアイテムの基底です。
///
/// <para>
/// 使用時間が経過した瞬間が、バニラの効果を差し替えられる唯一の地点です。
/// Sliced がそこを <see cref="CustomItem.OnUseCompleting"/> として渡してくれるので、
/// 効果は <see cref="OnUse"/> に書きます。差し止めた場合
/// <see cref="CustomItem.OnUsed"/> は呼ばれません。
/// </para>
/// <para>
/// 残り使用回数は <b>このインスタンスのフィールド</b>です。カスタムアイテムはアイテム 1 個につき
/// 1 インスタンスなので、シリアルをキーにした辞書は要りません。
/// </para>
/// </summary>
public abstract class CustomUsable : CustomItem
{
    /// <summary>アドレナリンと同じ AHP の減衰率です。</summary>
    private const float AhpDecay = 1.2f;

    /// <summary>アドレナリンと同じ AHP の吸収効率です。</summary>
    private const float AhpEfficacy = 0.7f;

    private int remainingUses = -1;

    /// <summary>
    /// 使用できる回数です。0 以下なら無制限。
    /// </summary>
    protected virtual int MaxUses => 1;

    /// <summary>
    /// バニラの使用効果を止めるか。false のときは <see cref="OnUse"/> の後にバニラの効果も出ます。
    /// </summary>
    protected virtual bool CancelVanillaUse => true;

    /// <summary>
    /// 使用回数を使い切ったらアイテムを消すか。
    /// </summary>
    protected virtual bool DestroyWhenDepleted => true;

    /// <summary>
    /// 残りの使用回数です。無制限なら <see cref="int.MaxValue"/>。
    /// </summary>
    public int RemainingUses
    {
        get
        {
            if (MaxUses <= 0) return int.MaxValue;
            if (remainingUses < 0) remainingUses = MaxUses;

            return remainingUses;
        }

        set
        {
            if (MaxUses <= 0) return;

            remainingUses = value < 0 ? 0 : value;
        }
    }

    /// <summary>
    /// 使用効果です。派生は基本ここだけ書きます。
    /// </summary>
    protected virtual void OnUse(Player player)
    {
    }

    /// <summary>
    /// 効果を出してよいか。false なら効果も消費も起きません。
    /// </summary>
    protected virtual bool CanUse(Player player) => true;

    /// <summary>
    /// AHP を足します。減衰と効率はバニラのアドレナリンに合わせてあります。
    /// </summary>
    protected static void AddArtificialHealth(Player player, float amount, float decay = AhpDecay) =>
        player.CreateAhpProcess(amount, player.MaxArtificialHealth, decay, AhpEfficacy, 0f, false);

    /// <summary>
    /// 使用回数を使い切ったときに呼ばれます。既定はアイテムを消します。
    ///
    /// <para>
    /// バニラの効果を差し止めると <c>Consumable.ServerOnUsingCompleted</c> ごと飛ばされるので、
    /// <b>アイテムはまだ手元に残っています</b>。自分で消す必要があります。
    /// 差し止めていない場合はバニラが消してくれるので、追跡を外すだけで足ります。
    /// </para>
    /// </summary>
    protected virtual void OnDepleted(Player player)
    {
        if (!DestroyWhenDepleted) return;

        if (CancelVanillaUse)
            Destroy();
        else
            Release();
    }

    protected override void OnUseCompleting(PlayerItemUsageEffectsApplyingEventArgs ev)
    {
        if (CancelVanillaUse)
        {
            ev.IsAllowed = false;

            // 差し止めると使用ストップウォッチが走ったまま残る。この状態でアイテムを取り除くと
            // Consumable.OnRemoved が「まだ効果を出していない」と判断してバニラの効果を焼き直すので、
            // 消す前にここで止めておく。
            if (ev.UsableItem.Base is Consumable consumable)
                consumable._useStopwatch.Reset();
        }

        Player player = ev.Player;

        if (!CanUse(player)) return;

        if (RemainingUses <= 0)
        {
            OnDepleted(player);

            return;
        }

        OnUse(player);

        if (MaxUses <= 0) return;

        remainingUses--;

        if (remainingUses <= 0)
            OnDepleted(player);
    }
}
