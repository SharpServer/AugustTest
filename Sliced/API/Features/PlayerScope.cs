using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;

using Logger = LabApi.Features.Console.Logger;

namespace Sliced.API.Features;

/// <summary>
/// プレイヤーに紐づくコルーチンと後始末をまとめて持つスコープです。
///
/// ここに載せたものは、プレイヤーの退出・ラウンド再開・明示的な <see cref="Dispose"/> で
/// 必ず止まります。そのため呼び出し側で ID を持ち回して <c>Player.Get(id)</c> し直したり、
/// コルーチンハンドルを自分で抱えたりする必要がありません。
/// </summary>
/// <example>
/// <code>
/// // 2 秒ごとに回復し、役職が終わったら自動で止まる
/// PlayerScope.Of(player).RunLoop(2f, p => p.Health += 5f);
///
/// // スコープが閉じるときに実行したい後始末
/// PlayerScope.Of(player).OnDispose(p => p.ClearInventory());
/// </code>
/// </example>
public sealed class PlayerScope : IDisposable
{
    private static readonly Dictionary<Player, PlayerScope> Scopes = new Dictionary<Player, PlayerScope>();

    private static bool hooked;

    private readonly List<CoroutineHandle> coroutines = [];
    private readonly List<Action<Player>> cleanups = [];

    private PlayerScope(Player player)
    {
        Player = player;
    }

    /// <summary>
    /// このスコープの持ち主です。
    /// </summary>
    public Player Player { get; }

    /// <summary>
    /// 破棄済みかどうか。
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// プレイヤーのスコープを取得します。無ければ作ります。
    /// </summary>
    public static PlayerScope Of(Player player)
    {
        if (player is null)
            throw new ArgumentNullException(nameof(player));

        if (Scopes.TryGetValue(player, out PlayerScope existing) && !existing.IsDisposed)
            return existing;

        PlayerScope scope = new PlayerScope(player);
        Scopes[player] = scope;

        Hook();

        return scope;
    }

    /// <summary>
    /// スコープが既にあるときだけ返します。無ければ null (作りません)。
    /// </summary>
    public static PlayerScope Find(Player player)
    {
        if (player is not null && Scopes.TryGetValue(player, out PlayerScope scope) && !scope.IsDisposed)
            return scope;

        return null;
    }

    /// <summary>
    /// プレイヤーのスコープを閉じます。無ければ何もしません。
    /// </summary>
    public static void Close(Player player) => Find(player)?.Dispose();

    /// <summary>
    /// すべてのスコープを閉じます。
    /// </summary>
    public static void CloseAll()
    {
        foreach (PlayerScope scope in new List<PlayerScope>(Scopes.Values))
        {
            scope.Dispose();
        }

        Scopes.Clear();
    }

    /// <summary>
    /// 全スコープを閉じ、静的イベント購読を解除します。
    /// </summary>
    public static void Shutdown()
    {
        CloseAll();

        if (!hooked) return;

        PlayerEvents.Left -= OnPlayerLeft;
        ServerEvents.RoundRestarted -= CloseAll;
        hooked = false;
    }

    /// <summary>
    /// 一定間隔で実行し続けます。プレイヤーが居なくなった時点で自動的に止まります。
    /// </summary>
    public CoroutineHandle RunLoop(float interval, Action<Player> tick)
    {
        if (tick is null)
            throw new ArgumentNullException(nameof(tick));

        return Track(Timing.RunCoroutine(Loop(interval, tick)));
    }

    /// <summary>
    /// 指定秒後に一度だけ実行します。
    /// </summary>
    public CoroutineHandle Delay(float delay, Action<Player> action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        return Track(Timing.CallDelayed(delay, () =>
        {
            if (IsDisposed || !IsAlive) return;

            Invoke(action);
        }));
    }

    /// <summary>
    /// 自前で起こしたコルーチンをこのスコープに預けます。破棄時に一緒に止まります。
    /// </summary>
    public CoroutineHandle Track(CoroutineHandle handle)
    {
        if (IsDisposed)
        {
            Timing.KillCoroutines(handle);

            return handle;
        }

        coroutines.Add(handle);

        return handle;
    }

    /// <summary>
    /// スコープが閉じるときに実行する後始末を登録します。登録の逆順で実行されます。
    /// </summary>
    public void OnDispose(Action<Player> cleanup)
    {
        if (cleanup is null)
            throw new ArgumentNullException(nameof(cleanup));

        if (IsDisposed)
        {
            Invoke(cleanup);

            return;
        }

        cleanups.Add(cleanup);
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        IsDisposed = true;

        foreach (CoroutineHandle handle in coroutines)
        {
            Timing.KillCoroutines(handle);
        }

        coroutines.Clear();

        // 1 つ落ちても残りを巻き込まないよう、後ろから 1 件ずつ。
        for (int i = cleanups.Count - 1; i >= 0; i--)
        {
            Invoke(cleanups[i]);
        }

        cleanups.Clear();
        Scopes.Remove(Player);
    }

    /// <summary>
    /// プレイヤーの ReferenceHub がまだ生きているか。
    /// LabApi の IsDestroyed が Unity の破棄判定 (!(UnityEngine.Object)ReferenceHub) をやってくれるので、
    /// netId を持ち回す必要はありません。
    /// </summary>
    private bool IsAlive => Player is { IsDestroyed: false };

    private IEnumerator<float> Loop(float interval, Action<Player> tick)
    {
        while (!IsDisposed && IsAlive)
        {
            yield return Timing.WaitForSeconds(interval);

            if (IsDisposed || !IsAlive) yield break;

            Invoke(tick);
        }
    }

    private void Invoke(Action<Player> action)
    {
        try
        {
            action(Player);
        }
        catch (Exception exception)
        {
            Logger.Error($"[Sliced] PlayerScope の処理で例外が発生しました: {exception}");
        }
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;

        PlayerEvents.Left += OnPlayerLeft;
        ServerEvents.RoundRestarted += CloseAll;
    }

    private static void OnPlayerLeft(PlayerLeftEventArgs ev) => Close(ev.Player);
}
