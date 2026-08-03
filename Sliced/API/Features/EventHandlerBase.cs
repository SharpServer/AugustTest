using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using Sliced.API.Enums;
using Sliced.API.Features.Attributes;
using Sliced.API.Interfaces;

namespace Sliced.API.Features;

/// <summary>
/// EXILED と LabApi の両方のイベントを購読できるイベントハンドラの基底クラスです。
///
/// 購読方法は 2 通りあり、どちらも同じインスタンスで併用できます。
/// 1. <see cref="RegisterEvents"/> / <see cref="UnregisterEvents"/> を override して
///    EXILED や LabApi の static イベントに += / -= する。
/// 2. <see cref="CustomEventsHandler"/> の OnXxx メソッドを override する
///    (LabApi 側で自動的に購読・解除される)。
///
/// 派生クラスを書くだけで <see cref="EventHandlerRegistry"/> が自動的に生成・購読します。
/// Plugin 側に登録処理を書く必要はありません。
/// 自分で生成したい場合は <see cref="NoAutoRegisterAttribute"/> を付けた上で
/// <see cref="Create{T}"/> や <see cref="Enable"/> を使ってください。
///
/// <see cref="Lifetime"/> や <see cref="Owner"/> を指定すれば、ラウンド再開時や
/// 所有プレイヤーの退出時に自動で <see cref="Dispose"/> されます。
/// </summary>
/// <example>
/// <code>
/// // これだけで購読される。どこにも登録処理を書く必要はない。
/// public class ABCHandler : EventHandlerBase
/// {
///     public override HandlerLifetime Lifetime => HandlerLifetime.Round;
///
///     // EXILED 側
///     public override void RegisterEvents()
///     {
///         Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
///     }
///
///     public override void UnregisterEvents()
///     {
///         Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
///     }
///
///     // LabApi 側 (CustomEventsHandler の override。登録・解除は不要)
///     public override void OnPlayerDeath(PlayerDeathEventArgs ev) { }
///
///     private void OnUsingItem(UsingItemEventArgs ev) { }
/// }
/// </code>
/// </example>
public abstract class EventHandlerBase : CustomEventsHandler, IDisposable
{
    private static readonly List<EventHandlerBase> ActiveHandlers = new List<EventHandlerBase>();

    private static bool lifetimeHooked;

    /// <summary>
    /// 現在有効になっているハンドラの一覧です。
    /// </summary>
    public static IReadOnlyList<EventHandlerBase> Active => ActiveHandlers;

    /// <summary>
    /// このハンドラが現在イベントを購読しているかどうか。
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// このハンドラが破棄済みかどうか。破棄後は <see cref="Enable"/> できません。
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// 自動破棄のタイミング。既定は <see cref="HandlerLifetime.Manual"/> (自動破棄しない)。
    /// </summary>
    public virtual HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <summary>
    /// このハンドラの所有プレイヤー。null 以外を返した場合、そのプレイヤーが
    /// サーバーから退出した時点で自動的に <see cref="Dispose"/> されます。
    /// 既定では <see cref="IPlayerOwn"/> を実装していればその Player を使います。
    /// </summary>
    public virtual Player Owner => (this as IPlayerOwn)?.Player;

    /// <summary>
    /// ハンドラを生成してそのまま <see cref="Enable"/> します。
    /// 自動登録を使わず、任意のタイミングで生成したい場合に使います。
    /// </summary>
    public static T Create<T>() where T : EventHandlerBase, new()
    {
        T handler = new T();
        handler.Enable();

        return handler;
    }

    /// <summary>
    /// 有効な全ハンドラを破棄します。
    /// 通常は <see cref="EventHandlerRegistry.Shutdown"/> から呼ばれます。
    /// </summary>
    public static void DisposeAll()
    {
        foreach (EventHandlerBase handler in ActiveHandlers.ToArray())
        {
            handler.Dispose();
        }
    }

    /// <summary>
    /// EXILED / LabApi の static イベントへの購読をここで書きます。
    /// <see cref="Enable"/> から 1 回だけ呼ばれます。
    /// </summary>
    public virtual void RegisterEvents()
    {
    }

    /// <summary>
    /// <see cref="RegisterEvents"/> で購読したものをここで解除します。
    /// <see cref="Disable"/> から 1 回だけ呼ばれます。
    /// </summary>
    public virtual void UnregisterEvents()
    {
    }

    /// <summary>
    /// イベントの購読を開始します。二重に呼んでも安全です。
    /// </summary>
    public void Enable()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);

        if (IsEnabled) return;

        // CustomEventsHandler の override 分は LabApi 側がリフレクションで購読する。
        CustomHandlersManager.RegisterEventsHandler(this);

        try
        {
            RegisterEvents();
        }
        catch (Exception exception)
        {
            // 途中で失敗した場合、購読済みのものを巻き戻してから投げ直す。
            SafeInvoke(UnregisterEvents, nameof(UnregisterEvents));
            CustomHandlersManager.UnregisterEventsHandler(this);
            Logger.Error($"[{GetType().Name}] {nameof(RegisterEvents)} に失敗しました: {exception}");

            throw;
        }

        IsEnabled = true;
        Track(this);
        SafeInvoke(OnEnabled, nameof(OnEnabled));
    }

    /// <summary>
    /// イベントの購読を解除します。二重に呼んでも安全です。
    /// </summary>
    public void Disable()
    {
        if (!IsEnabled) return;

        // 解除処理中に再入しても走らないよう、先にフラグを落とす。
        IsEnabled = false;

        SafeInvoke(UnregisterEvents, nameof(UnregisterEvents));
        CustomHandlersManager.UnregisterEventsHandler(this);
        Untrack(this);
        SafeInvoke(OnDisabled, nameof(OnDisabled));
    }

    /// <summary>
    /// 購読を解除してこのハンドラを破棄します。二重に呼んでも安全です。
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed) return;

        Disable();
        IsDisposed = true;
        SafeInvoke(OnDisposed, nameof(OnDisposed));
    }

    /// <summary>
    /// 購読完了後に呼ばれます。
    /// </summary>
    protected virtual void OnEnabled()
    {
    }

    /// <summary>
    /// 購読解除後に呼ばれます。
    /// </summary>
    protected virtual void OnDisabled()
    {
    }

    /// <summary>
    /// 破棄時に呼ばれます。ハンドラ以外のリソース (コルーチン・生成物など) の後始末に使います。
    /// </summary>
    protected virtual void OnDisposed()
    {
    }

    /// <summary>
    /// 後始末系の処理は 1 つ落ちても他を巻き込まないようにする。
    /// </summary>
    private void SafeInvoke(Action action, string name)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Logger.Error($"[{GetType().Name}] {name} で例外が発生しました: {exception}");
        }
    }

    private static void Track(EventHandlerBase handler)
    {
        ActiveHandlers.Add(handler);

        if (lifetimeHooked) return;

        // 自動破棄用のフックは EXILED 非導入でも動くよう LabApi 側だけを使う。
        ServerEvents.RoundRestarted += OnLifetimeRoundRestarted;
        PlayerEvents.Left += OnLifetimePlayerLeft;
        lifetimeHooked = true;
    }

    private static void Untrack(EventHandlerBase handler)
    {
        ActiveHandlers.Remove(handler);

        if (!lifetimeHooked || ActiveHandlers.Count > 0) return;

        ServerEvents.RoundRestarted -= OnLifetimeRoundRestarted;
        PlayerEvents.Left -= OnLifetimePlayerLeft;
        lifetimeHooked = false;
    }

    private static void OnLifetimeRoundRestarted()
    {
        foreach (EventHandlerBase handler in ActiveHandlers.ToArray())
        {
            if (handler.Lifetime is HandlerLifetime.Round)
            {
                handler.Dispose();
            }
        }
    }

    private static void OnLifetimePlayerLeft(PlayerLeftEventArgs ev)
    {
        if (ev.Player is null) return;

        foreach (EventHandlerBase handler in ActiveHandlers.ToArray())
        {
            if (handler.Owner is { } owner && owner.ReferenceHub == ev.Player.ReferenceHub)
            {
                handler.Dispose();
            }
        }
    }
}
