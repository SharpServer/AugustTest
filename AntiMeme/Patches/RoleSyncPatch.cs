using System;
using System.Collections.Generic;
using System.Linq;
using CentralAuth;
using HarmonyLib;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.SpawnData;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Patches;

/// <summary>
/// ロールシンク送信のタイミングを制御する定数です。実測で決めた値なので勝手に縮めないでください。
/// </summary>
internal static class RoleSyncTimings
{
    /// <summary>次フレーム相当。0 秒遅延は同フレーム扱いになるので最低値として使います。</summary>
    public const float NextFrame = 0.02f;

    /// <summary>同じ相手へのロールシンクを連投しない最小間隔。</summary>
    public const float MinSendInterval = 0.2f;

    /// <summary>初回ロールパックの再試行間隔。</summary>
    public const float InitialPackRetryInterval = 0.2f;

    /// <summary>ReadyClient になってから初回ロールパックを送るまでの落ち着き時間。</summary>
    public const float InitialPackReadySettle = 0.5f;

    /// <summary>初回ロールパックを諦めるまでの上限。</summary>
    public const float InitialPackMaxWait = 30f;
}

/// <summary>
/// <see cref="PlayerRoleManager.SendNewRoleInfo"/> を置き換えます。
///
/// <para>
/// バニラは全ハブへ無条件に <see cref="RoleSyncInfo"/> を投げます。
/// スポーン途中・未認証・接続が ready でないハブが混ざると
/// <c>connectionToClient</c> が null だったり、受信側の <c>NetworkReader</c> がずれて
/// 「Unknown message id」で切断されたりします。ここで受信側を選別し、
/// 1 人が落ちても他の送信を巻き込まないようにします。
/// </para>
/// <para>
/// 同じ相手へ短時間に何度も投げると、クライアント側のロール初期化が
/// 前のパケットの処理中に走って壊れます。<see cref="RoleSyncTimings.MinSendInterval"/> で
/// 間引き、間に合わなかった分は最新の 1 件だけ遅延送信します。
/// </para>
/// <para>
/// スロットリングの鍵に <c>netId</c> を使うのは、ここが
/// 「プレイヤーではなく Mirror のオブジェクトとしての宛先」を数えているからです。
/// </para>
/// </summary>
[HarmonyPatch(typeof(PlayerRoleManager), nameof(PlayerRoleManager.SendNewRoleInfo))]
public static class RoleSyncPatch
{
    private static readonly Dictionary<uint, LastSend> LastSendByTarget = new Dictionary<uint, LastSend>();
    private static readonly Dictionary<uint, ReferenceHub> PendingByTarget = new Dictionary<uint, ReferenceHub>();

    [HarmonyPrefix]
    private static bool SendNewRoleInfoPrefix(PlayerRoleManager __instance)
    {
        SendOrDefer(__instance);

        return false;
    }

    /// <summary>
    /// ラウンド再開時に間引き状態を捨てます。遅延中の送信はスコープが閉じることで止まります。
    /// </summary>
    internal static void Reset()
    {
        LastSendByTarget.Clear();
        PendingByTarget.Clear();
    }

    private static void SendOrDefer(PlayerRoleManager manager)
    {
        if (!NetworkServer.active) return;
        if (!TryGetTarget(manager, out ReferenceHub targetHub, out uint targetNetId)) return;

        float now = Time.realtimeSinceStartup;

        if (LastSendByTarget.TryGetValue(targetNetId, out LastSend last) &&
            ReferenceEquals(last.Hub, targetHub))
        {
            float elapsed = now - last.Time;

            if (elapsed < RoleSyncTimings.MinSendInterval)
            {
                Defer(targetHub, targetNetId, RoleSyncTimings.MinSendInterval - elapsed);

                return;
            }
        }

        SendNow(manager, targetHub, targetNetId);
    }

    /// <summary>
    /// 同じ宛先に対して遅延送信は 1 件だけ持ちます。後から来たものが古いものを置き換えます。
    /// </summary>
    private static void Defer(ReferenceHub expectedHub, uint targetNetId, float delay)
    {
        if (PendingByTarget.TryGetValue(targetNetId, out ReferenceHub pending) &&
            ReferenceEquals(pending, expectedHub))
        {
            return;
        }

        PendingByTarget[targetNetId] = expectedHub;

        RoundScope.Current.Delay(Mathf.Max(RoleSyncTimings.NextFrame, delay), () =>
        {
            if (!PendingByTarget.TryGetValue(targetNetId, out ReferenceHub current) ||
                !ReferenceEquals(current, expectedHub))
            {
                return;
            }

            PendingByTarget.Remove(targetNetId);

            // netId は使い回されるので、同じ netId が別のハブを指していないか必ず確かめる。
            if (!ReferenceHub.TryGetHubNetID(targetNetId, out ReferenceHub targetHub) ||
                !ReferenceEquals(targetHub, expectedHub) ||
                targetHub.roleManager is null)
            {
                return;
            }

            SendOrDefer(targetHub.roleManager);
        });
    }

    private static void SendNow(PlayerRoleManager manager, ReferenceHub targetHub, uint targetNetId)
    {
        LastSendByTarget[targetNetId] = new LastSend(targetHub, Time.realtimeSinceStartup);

        // 送信中にハブ一覧が変わり得るのでコピーしてから回す。
        foreach (ReferenceHub receiverHub in ReferenceHub.AllHubs.ToArray())
        {
            TrySendToReceiver(manager, targetHub, receiverHub);
        }
    }

    private static void TrySendToReceiver(PlayerRoleManager manager, ReferenceHub targetHub, ReferenceHub receiverHub)
    {
        if (!NetGuards.IsValidReceiver(receiverHub)) return;

        NetworkWriterPooled writer = null;

        try
        {
            RoleTypeId targetRole = FpcServerPositionDistributor.GetVisibleRole(receiverHub, targetHub);
            writer = NetworkWriterPool.Get();

            if (FpcServerPositionDistributor.InvokeRoleSyncEvent(targetHub, receiverHub, targetRole, writer) is { } spoofed)
                targetRole = spoofed;

            if (targetRole is RoleTypeId.None or RoleTypeId.Destroyed) return;

            receiverHub.connectionToClient.Send(new RoleSyncInfo(targetHub, targetRole, receiverHub, writer), channelId: 0);
            manager.PreviouslySentRole[receiverHub.netId] = targetRole;
        }
        catch (Exception exception)
        {
            // 1 人の受信者で落ちても残りの配信は続ける。
            Logger.Warn($"[RoleSync] 送信に失敗しました target={Describe(targetHub)} receiver={Describe(receiverHub)}: {exception}");
        }
        finally
        {
            if (writer is not null)
                NetworkWriterPool.Return(writer);
        }
    }

    /// <summary>
    /// 送信元として妥当かを見ます。受信側と違い、対象は接続が ready でなくても構いません
    /// (「まだ繋がっていない人のロール」を他人へ配るのは正常な動作です)。
    /// </summary>
    private static bool TryGetTarget(PlayerRoleManager manager, out ReferenceHub targetHub, out uint targetNetId)
    {
        targetHub = null;
        targetNetId = 0;

        if (manager is null) return false;

        targetHub = manager.Hub;

        if (targetHub is null || targetHub.roleManager is null) return false;
        if (targetHub.isLocalPlayer) return false;
        if (targetHub.Mode == ClientInstanceMode.Unverified) return false;

        PlayerRoleBase currentRole = targetHub.roleManager.CurrentRole;

        if (currentRole is null || currentRole.RoleTypeId is RoleTypeId.None or RoleTypeId.Destroyed) return false;

        targetNetId = targetHub.netId;

        return targetNetId != 0;
    }

    internal static string Describe(ReferenceHub hub) =>
        hub is null ? "<null>" : $"{hub.nicknameSync.MyNick}#{hub.PlayerId}/{hub.netId}";

    private readonly struct LastSend
    {
        public LastSend(ReferenceHub hub, float time)
        {
            Hub = hub;
            Time = time;
        }

        public ReferenceHub Hub { get; }

        public float Time { get; }
    }
}

/// <summary>
/// <see cref="PlayerRolesNetUtils.HandleSpawnedPlayer"/> を置き換えます。
///
/// <para>
/// バニラはハブが <c>ReferenceHub.OnPlayerAdded</c> に載った瞬間に
/// <see cref="RoleSyncInfoPack"/> を送りますが、その時点のクライアントはまだ
/// <see cref="ClientInstanceMode.ReadyClient"/> ではありません。
/// 受け取れない相手に投げると初回のロール情報が丸ごと落ち、
/// 「全員が観戦者に見える」状態で入場することになります。
/// ready になるまで待ってから送り、上限を過ぎたら諦めます。
/// </para>
/// </summary>
[HarmonyPatch(typeof(PlayerRolesNetUtils), nameof(PlayerRolesNetUtils.HandleSpawnedPlayer))]
public static class RoleSyncInitialPackPatch
{
    private static readonly Dictionary<uint, ReferenceHub> PendingByReceiver = new Dictionary<uint, ReferenceHub>();

    [HarmonyPrefix]
    private static bool HandleSpawnedPlayerPrefix(ReferenceHub hub)
    {
        // クライアント側の処理はバニラに任せる。
        if (!NetworkServer.active) return true;

        if (hub is not null && hub.netId != 0)
        {
            TrySend(hub.netId, hub, Time.realtimeSinceStartup + RoleSyncTimings.InitialPackMaxWait, 0f);
        }

        return false;
    }

    internal static void Reset() => PendingByReceiver.Clear();

    private static void TrySend(uint receiverNetId, ReferenceHub expectedHub, float deadline, float readySince)
    {
        if (!ReferenceHub.TryGetHubNetID(receiverNetId, out ReferenceHub hub) ||
            !ReferenceEquals(hub, expectedHub) ||
            hub.isLocalPlayer)
        {
            return;
        }

        if (!NetGuards.IsReadyClient(hub))
        {
            if (Time.realtimeSinceStartup >= deadline)
            {
                Logger.Warn($"[RoleSync] 初回ロールパックを諦めました。受信側が ready になりません: {RoleSyncPatch.Describe(hub)}");

                return;
            }

            Retry(receiverNetId, expectedHub, deadline, 0f);

            return;
        }

        if (readySince <= 0f)
            readySince = Time.realtimeSinceStartup;

        // ready になった直後はまだシーン読み込みが走っている。落ち着くまで待つ。
        if (Time.realtimeSinceStartup - readySince < RoleSyncTimings.InitialPackReadySettle)
        {
            Retry(receiverNetId, expectedHub, deadline, readySince);

            return;
        }

        try
        {
            hub.connectionToClient.Send(new RoleSyncInfoPack(hub), channelId: 0);
        }
        catch (Exception exception)
        {
            Logger.Warn($"[RoleSync] 初回ロールパックの送信に失敗しました {RoleSyncPatch.Describe(hub)}: {exception}");
        }
    }

    private static void Retry(uint receiverNetId, ReferenceHub expectedHub, float deadline, float readySince)
    {
        if (PendingByReceiver.TryGetValue(receiverNetId, out ReferenceHub pending) &&
            ReferenceEquals(pending, expectedHub))
        {
            return;
        }

        PendingByReceiver[receiverNetId] = expectedHub;

        RoundScope.Current.Delay(RoleSyncTimings.InitialPackRetryInterval, () =>
        {
            if (!PendingByReceiver.TryGetValue(receiverNetId, out ReferenceHub current) ||
                !ReferenceEquals(current, expectedHub))
            {
                return;
            }

            PendingByReceiver.Remove(receiverNetId);
            TrySend(receiverNetId, expectedHub, deadline, readySince);
        });
    }
}

/// <summary>
/// <see cref="RoleSyncInfoPack"/> の書き出しを置き換えます。
///
/// <para>
/// バニラは <c>ReferenceHub.AllHubs.Count</c> をそのまま件数として書いてから
/// 全ハブぶんを書き出します。途中の 1 件で例外が出ると、宣言した件数と実際の件数が
/// ずれたままパケットが飛び、受信側の <c>NetworkReader</c> が以降ずっと壊れます
/// (症状は「Unknown message id」や無関係な OnDeserialize 失敗)。
/// ここでは先に書ける分だけ組み立ててから件数を書きます。
/// </para>
/// </summary>
[HarmonyPatch(typeof(PlayerRolesNetUtils), nameof(PlayerRolesNetUtils.WriteRoleSyncInfoPack))]
public static class RoleSyncInfoPackWriterPatch
{
    [HarmonyPrefix]
    private static bool WriteRoleSyncInfoPackPrefix(NetworkWriter writer, RoleSyncInfoPack info)
    {
        try
        {
            Write(writer, info._receiverHub);
        }
        catch (Exception exception)
        {
            // ここで投げると件数だけ書かれた壊れたパケットが飛ぶ。空パックとして閉じる。
            Logger.Warn($"[RoleSync] ロールパックの書き出しに失敗しました: {exception}");
            writer.WriteUShort(0);
        }

        return false;
    }

    private static void Write(NetworkWriter writer, ReferenceHub receiverHub)
    {
        if (receiverHub is null || receiverHub.netId == 0)
        {
            writer.WriteUShort(0);

            return;
        }

        uint receiverNetId = receiverHub.netId;
        List<Payload> payloads = new List<Payload>();

        foreach (ReferenceHub targetHub in ReferenceHub.AllHubs.ToArray())
        {
            if (TryCreatePayload(receiverHub, targetHub) is { } payload)
                payloads.Add(payload);
        }

        writer.WriteUShort((ushort)payloads.Count);

        foreach (Payload payload in payloads)
        {
            foreach (byte value in payload.Bytes)
            {
                writer.WriteByte(value);
            }

            payload.TargetHub.roleManager.PreviouslySentRole[receiverNetId] = payload.Role;
        }
    }

    private static Payload TryCreatePayload(ReferenceHub receiverHub, ReferenceHub targetHub)
    {
        NetworkWriterPooled payloadWriter = null;

        try
        {
            if (!IsValidTarget(receiverHub, targetHub)) return null;
            if (!TryGetRole(receiverHub, targetHub, out RoleTypeId targetRole)) return null;

            payloadWriter = NetworkWriterPool.Get();
            new RoleSyncInfo(targetHub, targetRole, receiverHub, null).Write(payloadWriter);

            ArraySegment<byte> segment = payloadWriter.ToArraySegment();
            byte[] copy = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, copy, 0, segment.Count);

            return new Payload(targetHub, targetRole, copy);
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"[RoleSync] ロールパックの 1 件を飛ばしました target={RoleSyncPatch.Describe(targetHub)} " +
                $"receiver={RoleSyncPatch.Describe(receiverHub)}: {exception}");

            return null;
        }
        finally
        {
            if (payloadWriter is not null)
                NetworkWriterPool.Return(payloadWriter);
        }
    }

    /// <summary>
    /// 受信側に広告するロールを決めます。
    /// 判定はゲーム本体の <see cref="FpcServerPositionDistributor.GetVisibleRole"/> に任せます。
    /// 毎ティックの配信 (<c>WriteAll</c>) と <see cref="PlayerRoleManager.SendNewRoleInfo"/> が
    /// 同じ関数を使っているので、初回パックだけ別基準にすると入場直後だけ見え方がずれます。
    /// </summary>
    private static bool TryGetRole(ReferenceHub receiverHub, ReferenceHub targetHub, out RoleTypeId targetRole)
    {
        targetRole = RoleTypeId.None;

        PlayerRoleBase currentRole = targetHub.roleManager.CurrentRole;

        if (currentRole is null || currentRole.RoleTypeId is RoleTypeId.None or RoleTypeId.Destroyed) return false;

        targetRole = FpcServerPositionDistributor.GetVisibleRole(receiverHub, targetHub);

        if (targetRole is RoleTypeId.None or RoleTypeId.Destroyed) return false;

        // RoleSyncInfo.Write は広告するロールに関係なく実ロールの IPublicSpawnDataWriter を必ず書くが、
        // PlayerRoleManager.InitializeNewRole は広告が Spectator のときスポーンデータを読み飛ばす。
        // つまり Spectator に偽装した瞬間、書いた分だけ受信側の読み位置がずれてパケット全体が壊れる。
        // スポーンデータを持たないロールは影響を受けないので、実際に壊れる組み合わせだけ落とす。
        return targetRole is not RoleTypeId.Spectator ||
               currentRole.RoleTypeId is RoleTypeId.Spectator ||
               currentRole is not IPublicSpawnDataWriter;
    }

    private static bool IsValidTarget(ReferenceHub receiverHub, ReferenceHub targetHub)
    {
        if (targetHub is null) return false;
        if (ReferenceEquals(receiverHub, targetHub)) return false;
        if (targetHub.roleManager is null) return false;
        if (targetHub.isLocalPlayer || targetHub.netId == 0) return false;
        if (targetHub.Mode == ClientInstanceMode.Unverified) return false;

        // 専用サーバー側のハブは接続を持たないので ready 判定を課さない。
        return targetHub.Mode == ClientInstanceMode.DedicatedServer || NetGuards.IsValidReceiver(targetHub);
    }

    private sealed class Payload
    {
        public Payload(ReferenceHub targetHub, RoleTypeId role, byte[] bytes)
        {
            TargetHub = targetHub;
            Role = role;
            Bytes = bytes;
        }

        public ReferenceHub TargetHub { get; }

        public RoleTypeId Role { get; }

        public byte[] Bytes { get; }
    }
}

/// <summary>
/// ラウンド再開でロールシンクの間引き状態を捨てます。
/// </summary>
public sealed class RoleSyncCleanupHandler : EventHandlerBase
{
    public override void OnServerRoundRestarted()
    {
        RoleSyncPatch.Reset();
        RoleSyncInitialPackPatch.Reset();
    }
}
