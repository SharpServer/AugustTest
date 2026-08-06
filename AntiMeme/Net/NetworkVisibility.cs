using System;
using System.Collections.Generic;
using AdminToys;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using Mirror;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Net;

/// <summary>
/// 1 つの <see cref="NetworkIdentity"/> を「誰に見せるか」です。
///
/// <para>
/// 既定は全員 Hide です。<see cref="Owner"/> を決めて初めて見えるようになります。
/// 見せる相手は所有者本人と、所有者を観戦している人だけです。
/// </para>
/// </summary>
public sealed class NetworkShowState
{
    /// <summary>このオブジェクトの持ち主です。null なら誰にも見えません。</summary>
    public Player Owner { get; set; }

    /// <summary>持ち主本人に見せるかどうか。</summary>
    public bool ShowToOwner { get; set; } = true;

    /// <summary>持ち主を観戦している人に見せるかどうか。</summary>
    public bool ShowToSpectators { get; set; } = true;

    /// <summary>
    /// この相手に見せるべきかどうか。
    /// </summary>
    public bool ShouldShow(Player viewer)
    {
        if (viewer is not { IsDestroyed: false }) return false;
        if (Owner is not { IsDestroyed: false }) return false;

        if (viewer.ReferenceHub == Owner.ReferenceHub) return ShowToOwner;

        return ShowToSpectators && viewer.CurrentlySpectating is { } spectated && spectated.ReferenceHub == Owner.ReferenceHub;
    }
}

/// <summary>
/// <see cref="NetworkIdentity"/> を持つ生成物 (Primitive / Light / スキマティックのブロックなど) を
/// 特定のプレイヤーにだけ見せるための仕組みです。
///
/// <para>
/// Mirror へ直接 Spawn/Hide メッセージを書き込むので、送信先は
/// <see cref="NetGuards.IsReadyClient(Player)"/> を通ったクライアントに限ります。
/// スポーン途中のハブや NPC に投げると壊れます。
/// </para>
/// <para>
/// Mirror はロール変更のたびに全 <see cref="NetworkIdentity"/> を再送信するため、
/// 入場時とスポーン後に管理中のオブジェクトを一括で当て直します。
/// </para>
/// </summary>
public static class NetworkVisibility
{
    private static readonly Dictionary<NetworkIdentity, NetworkShowState> States =
        new Dictionary<NetworkIdentity, NetworkShowState>();

    private static readonly List<NetworkIdentity> DeadIdentities = new List<NetworkIdentity>();

    /// <summary>
    /// Show を最優先で拒否する追加判定です。true を返した組み合わせは表示されません。
    ///
    /// <para>
    /// 装着物の透明化のように「このマネージャの外側で隠している」ものが、
    /// Mirror の再送信や状態の当て直しで勝手に見えてしまうのを防ぐための口です。
    /// </para>
    /// </summary>
    public static Func<NetworkIdentity, Player, bool> ShowVeto { get; set; }

    /// <summary>
    /// 表示管理に登録します。Spawn した直後に呼んでください。
    /// </summary>
    public static void InitShowState(this NetworkIdentity identity, NetworkShowState state = null)
    {
        if (identity == null) return;

        States[identity] = state ?? new NetworkShowState();
        identity.ApplyShowState();
    }

    /// <summary>
    /// 表示管理から外します。オブジェクトを壊す前に呼んでください。
    /// </summary>
    public static void RemoveShowState(this NetworkIdentity identity)
    {
        if (identity == null) return;

        States.Remove(identity);
    }

    /// <summary>
    /// 現在の状態です。管理していなければ null。
    /// </summary>
    public static NetworkShowState GetShowState(this NetworkIdentity identity) =>
        identity != null && States.TryGetValue(identity, out NetworkShowState state) ? state : null;

    /// <summary>
    /// 状態を書き換えて、その場で全員に当て直します。未登録なら新しく作ります。
    /// </summary>
    public static void UpdateShowState(this NetworkIdentity identity, Action<NetworkShowState> mutate)
    {
        if (identity == null || mutate is null) return;

        if (!States.TryGetValue(identity, out NetworkShowState state))
        {
            state = new NetworkShowState();
            States[identity] = state;
        }

        mutate(state);
        identity.ApplyShowState();
    }

    /// <summary>
    /// このオブジェクトの表示状態を全員に当て直します。
    /// </summary>
    public static void ApplyShowState(this NetworkIdentity identity)
    {
        if (identity == null || !States.TryGetValue(identity, out NetworkShowState state)) return;

        foreach (Player viewer in Player.List)
        {
            Send(identity, viewer, state.ShouldShow(viewer));
        }
    }

    /// <summary>
    /// 指定したプレイヤーに、管理中の全オブジェクトを当て直します。
    /// </summary>
    public static void RefreshPlayer(Player viewer)
    {
        if (viewer is not { IsDestroyed: false }) return;

        PruneDead();

        foreach (KeyValuePair<NetworkIdentity, NetworkShowState> entry in States)
        {
            Send(entry.Key, viewer, entry.Value.ShouldShow(viewer));
        }
    }

    /// <summary>
    /// 管理をすべて捨てます。オブジェクト自体は所有者が始末します。
    /// </summary>
    public static void Clear()
    {
        States.Clear();
        ShowVeto = null;
    }

    /// <summary>
    /// 指定したプレイヤーにだけこのオブジェクトを見せます。
    /// </summary>
    public static void ShowNetworkIdentity(this Player viewer, NetworkIdentity identity)
    {
        if (identity == null || !NetGuards.IsReadyClient(viewer)) return;
        if (IsVetoed(identity, viewer)) return;

        NetworkServer.ShowForConnection(identity, viewer.Connection);
    }

    /// <summary>
    /// 指定したプレイヤーからこのオブジェクトを隠します。
    /// </summary>
    public static void HideNetworkIdentity(this Player viewer, NetworkIdentity identity)
    {
        if (identity == null || !NetGuards.IsReadyClient(viewer)) return;

        NetworkServer.HideForConnection(identity, viewer.Connection);
    }

    /// <inheritdoc cref="InitShowState(NetworkIdentity, NetworkShowState)"/>
    public static void InitShowState(this AdminToyBase toy, NetworkShowState state = null) =>
        toy?.netIdentity.InitShowState(state);

    /// <inheritdoc cref="RemoveShowState(NetworkIdentity)"/>
    public static void RemoveShowState(this AdminToyBase toy) => toy?.netIdentity.RemoveShowState();

    /// <inheritdoc cref="GetShowState(NetworkIdentity)"/>
    public static NetworkShowState GetShowState(this AdminToyBase toy) => toy?.netIdentity.GetShowState();

    /// <inheritdoc cref="UpdateShowState(NetworkIdentity, Action{NetworkShowState})"/>
    public static void UpdateShowState(this AdminToyBase toy, Action<NetworkShowState> mutate) =>
        toy?.netIdentity.UpdateShowState(mutate);

    /// <inheritdoc cref="ApplyShowState(NetworkIdentity)"/>
    public static void ApplyShowState(this AdminToyBase toy) => toy?.netIdentity.ApplyShowState();

    /// <summary>
    /// 表示管理から外してから壊します。管理表に死んだ参照を残さないための入口です。
    /// </summary>
    public static void SafeDestroy(this AdminToyBase toy)
    {
        if (toy == null) return;

        toy.RemoveShowState();
        NetworkServer.Destroy(toy.gameObject);
    }

    /// <inheritdoc cref="InitShowState(NetworkIdentity, NetworkShowState)"/>
    public static void InitShowState(this IEnumerable<NetworkIdentity> identities, NetworkShowState state = null)
    {
        foreach (NetworkIdentity identity in identities)
        {
            identity.InitShowState(state);
        }
    }

    /// <inheritdoc cref="RemoveShowState(NetworkIdentity)"/>
    public static void RemoveShowState(this IEnumerable<NetworkIdentity> identities)
    {
        foreach (NetworkIdentity identity in identities)
        {
            identity.RemoveShowState();
        }
    }

    /// <inheritdoc cref="ApplyShowState(NetworkIdentity)"/>
    public static void ApplyShowState(this IEnumerable<NetworkIdentity> identities)
    {
        foreach (NetworkIdentity identity in identities)
        {
            identity.ApplyShowState();
        }
    }

    private static void Send(NetworkIdentity identity, Player viewer, bool show)
    {
        if (show) viewer.ShowNetworkIdentity(identity);
        else viewer.HideNetworkIdentity(identity);
    }

    private static bool IsVetoed(NetworkIdentity identity, Player viewer)
    {
        if (ShowVeto is not { } veto) return false;

        try
        {
            return veto(identity, viewer);
        }
        catch (Exception exception)
        {
            Logger.Warn($"[NetworkVisibility] ShowVeto で例外が発生しました: {exception}");

            return true;
        }
    }

    /// <summary>
    /// 壊れた <see cref="NetworkIdentity"/> を管理表から落とします。
    /// 呼び忘れた <see cref="RemoveShowState(NetworkIdentity)"/> の受け皿です。
    /// </summary>
    private static void PruneDead()
    {
        foreach (NetworkIdentity identity in States.Keys)
        {
            if (identity == null)
                DeadIdentities.Add(identity);
        }

        foreach (NetworkIdentity identity in DeadIdentities)
        {
            States.Remove(identity);
        }

        DeadIdentities.Clear();
    }
}

/// <summary>
/// Mirror が全オブジェクトを再送信する場面で、管理中の表示状態を当て直します。
/// </summary>
public sealed class NetworkVisibilityHandler : EventHandlerBase
{
    public override void OnServerRoundRestarted() => NetworkVisibility.Clear();

    public override void OnPlayerJoined(PlayerJoinedEventArgs ev) => NetworkVisibility.RefreshPlayer(ev.Player);

    public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
    {
        Player player = ev.Player;

        // ロール変更の直後は Mirror の再送信がこの後に走る。1 フレーム待ってから当て直す。
        RoundScope.Current.Delay(0f, () => NetworkVisibility.RefreshPlayer(player));
    }

    public override void OnPlayerChangedSpectator(PlayerChangedSpectatorEventArgs ev) =>
        NetworkVisibility.RefreshPlayer(ev.Player);
}
