using System.Collections.Generic;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Net;

/// <summary>
/// 演出・武装・チーム表現のために<b>こちらが生成した</b> NPC の一覧です。
///
/// <para>
/// 勝利判定・生存者集計・観戦保護から外す相手を選ぶときは、
/// <c>IsNpc</c> のようなヒューリスティックではなくここを見てください。
/// カスタム役職・タレット・ヒットボックス・スキマティック相互作用は
/// <b>意図的に NPC を使う</b>ので、<c>IsNpc</c> で弾くと巻き添えで壊れます。
/// 「自分で作ったものだけ覚えておく」のが唯一安全な判定です。
/// </para>
/// </summary>
public static class InternalNpcs
{
    private static readonly Dictionary<ReferenceHub, InternalNpcKind> Managed =
        new Dictionary<ReferenceHub, InternalNpcKind>();

    /// <summary>
    /// 生成した NPC を登録します。破棄するときは必ず <see cref="Unregister"/> を呼んでください。
    /// </summary>
    public static void Register(Player npc, InternalNpcKind kind)
    {
        if (npc is not { IsDestroyed: false }) return;

        Managed[npc.ReferenceHub] = kind;
    }

    /// <inheritdoc cref="Register(Player, InternalNpcKind)"/>
    public static void Register(ReferenceHub hub, InternalNpcKind kind)
    {
        if (hub is null) return;

        Managed[hub] = kind;
    }

    /// <summary>
    /// 登録を解除します。NPC を破棄する直前に呼んでください。
    /// </summary>
    public static void Unregister(Player npc)
    {
        if (npc is null) return;

        Managed.Remove(npc.ReferenceHub);
    }

    /// <inheritdoc cref="Unregister(Player)"/>
    public static void Unregister(ReferenceHub hub)
    {
        if (hub is null) return;

        Managed.Remove(hub);
    }

    /// <summary>
    /// こちらが生成した NPC かどうか。
    /// </summary>
    public static bool IsManaged(Player player) =>
        player is not null && Managed.ContainsKey(player.ReferenceHub);

    /// <inheritdoc cref="IsManaged(Player)"/>
    public static bool IsManaged(ReferenceHub hub) =>
        hub is not null && Managed.ContainsKey(hub);

    /// <summary>
    /// 指定した用途で生成した NPC かどうか。
    /// </summary>
    public static bool Is(Player player, InternalNpcKind kind) =>
        player is not null &&
        Managed.TryGetValue(player.ReferenceHub, out InternalNpcKind registered) &&
        registered == kind;

    /// <summary>
    /// 実際に接続している人間かどうか。<b>人数を数えるときは必ずこれを使ってください。</b>
    /// </summary>
    /// <remarks>
    /// <see cref="LabApi.Features.Wrappers.Player.IsPlayer"/> だけではホストとダミーしか外れず、
    /// こちらが演出のために立てている NPC が残ります。ロビーの「◯ / ◯ 人」や
    /// 開始人数の判定にそのまま使うと、NPC のぶんまで数に入ります。
    /// </remarks>
    public static bool IsHuman(Player player) =>
        player is { IsDestroyed: false, IsPlayer: true } && !IsManaged(player);

    /// <summary>
    /// 「サーバーが動かしている内部 NPC でもホストでもない、実在の参加者」かどうか。
    /// 位置追従の対象を選ぶときに使います。
    /// </summary>
    public static bool IsParticipant(Player player)
    {
        if (player is not { IsDestroyed: false }) return false;
        if (player.IsHost) return false;
        if (IsManaged(player.ReferenceHub)) return false;

        return player.IsNpc || player.IsReady;
    }

    /// <summary>
    /// 登録をすべて捨てます。ラウンド再開時に呼ばれます。
    /// </summary>
    public static void Clear() => Managed.Clear();
}

/// <summary>
/// 内部 NPC の用途です。振る舞いを持たない閉じた値集合なので enum のままにしています。
/// </summary>
public enum InternalNpcKind
{
    /// <summary>HID タレット。誤ってカサンドラの死亡アナウンスを出さないために区別します。</summary>
    HidTurret,

    /// <summary>触手など、当たり判定を持つ演出物。</summary>
    Tentacle,

    /// <summary>チーム表現のために立てている見せかけの隊員。</summary>
    TeamNpc,
}

/// <summary>
/// ラウンド再開で登録を捨てます。NPC 自体はそれぞれの所有者が始末します。
/// </summary>
public sealed class InternalNpcCleanupHandler : EventHandlerBase
{
    public override void OnServerRoundRestarted() => InternalNpcs.Clear();
}
