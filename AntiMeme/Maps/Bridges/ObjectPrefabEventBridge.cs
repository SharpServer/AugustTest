using System;
using LabApi.Events.Arguments.PlayerEvents;
using Sliced.API.Enums;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Maps.Bridges;

/// <summary>
/// Interactable の操作イベントを、対応する <see cref="InteractableHandle"/> と
/// その持ち主の <see cref="ObjectPrefab"/> へ配ります。
///
/// <para>
/// 対応付けは Toy の実体を鍵にした表なので、当たったのがどの Interactable かは一意に決まります。
/// 旧実装にあった「半径内の Prefab すべてに通知する」フォールバックは、
/// 半径を設定している Prefab が 1 つも無かったため落としました。
/// </para>
/// <para>
/// 例外の受け止めはここ 1 か所です。マップの操作 1 回でサーバー側のイベント配送を
/// 止めてしまわないよう、ハンドラが落ちてもログにして続行します。
/// </para>
/// </summary>
public sealed class ObjectPrefabEventBridge : EventHandlerBase
{
    /// <inheritdoc/>
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <inheritdoc/>
    public override void OnPlayerSearchingToy(PlayerSearchingToyEventArgs ev)
    {
        if (!InteractableHandle.TryRoute(ev.Interactable, out InteractableHandle handle)) return;

        Dispatch(handle, () =>
        {
            handle.RaiseInteracting(ev);
            handle.Owner.OnInteracting(ev);
        },
        ev);
    }

    /// <inheritdoc/>
    public override void OnPlayerSearchedToy(PlayerSearchedToyEventArgs ev) => Complete(ev);

    /// <summary>
    /// 長押し時間 0 の Interactable は <see cref="PlayerSearchedToyEventArgs"/> を通らず
    /// こちらだけが飛んでくるので、同じ「操作完了」として扱います。
    /// </summary>
    public override void OnPlayerInteractedToy(PlayerInteractedToyEventArgs ev)
        => Complete(new PlayerSearchedToyEventArgs(ev.Player.ReferenceHub, ev.Interactable.Base));

    private static void Complete(PlayerSearchedToyEventArgs ev)
    {
        if (!InteractableHandle.TryRoute(ev.Interactable, out InteractableHandle handle)) return;

        Dispatch(handle, () =>
        {
            handle.RaiseInteracted(ev);
            handle.Owner.OnInteracted(ev);
        },
        null);
    }

    private static void Dispatch(InteractableHandle handle, Action action, PlayerSearchingToyEventArgs cancellable)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            // 途中で落ちた操作は成立させない。
            if (cancellable is not null)
                cancellable.IsAllowed = false;

            Logger.Error(
                $"[{handle.Owner.GetType().Name}] Interactable '{handle.Key}' の処理で例外が発生しました: {exception}");
        }
    }
}
