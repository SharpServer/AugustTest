using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Sliced.API.Features;

using ExiledMap = Exiled.Events.Handlers.Map;
using AnnouncingScpTerminationEventArgs = Exiled.Events.EventArgs.Map.AnnouncingScpTerminationEventArgs;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP が死んだときの終了放送を出します。
/// </summary>
/// <remarks>
/// <para>
/// 購読を 1 本にまとめてあるのは、各役職の <c>OnSpawned</c> に相乗りすると
/// <c>base.OnSpawned()</c> を書き忘れた役職だけ黙って放送されなくなるためです。
/// 放送するかどうかは役職側の <see cref="ScpRole.CassieName"/> が決めます。
/// </para>
/// <para>
/// 自前で放送したら<b>バニラの終了放送を必ず握り潰します</b>。
/// ゲーム本体は死亡時に <c>CassieScpTerminationAnnouncement</c> を別途キューへ積むので、
/// 放置すると自前の放送 (キューを Clear してから積む) の後ろにバニラの分が繋がって、
/// 「SCP-106 が終了されました」が 2 回流れます。
/// </para>
/// </remarks>
public sealed class ScpTerminationAnnouncer : EventHandlerBase
{
    /// <summary>
    /// 自前で放送済みのプレイヤーです。バニラ放送は死亡の直後に 1 回だけ来るので、
    /// 握り潰したその場で外します。
    /// </summary>
    private static readonly HashSet<ReferenceHub> Announced = new HashSet<ReferenceHub>();

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        PlayerEvents.Dying += OnDying;
        ExiledMap.AnnouncingScpTermination += OnAnnouncingScpTermination;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        PlayerEvents.Dying -= OnDying;
        ExiledMap.AnnouncingScpTermination -= OnAnnouncingScpTermination;

        Announced.Clear();
    }

    /// <inheritdoc/>
    public override void OnServerRoundRestarted() => Announced.Clear();

    private static void OnDying(PlayerDyingEventArgs ev)
    {
        if (!ev.IsAllowed) return;

        if (CustomRole.Of(ev.Player) is not ScpRole { CassieName: not null } scp) return;

        scp.AnnounceTermination(ev);

        if (ev.Player.ReferenceHub is { } hub)
            Announced.Add(hub);
    }

    /// <summary>自前で放送した分のバニラ放送を止めます。</summary>
    private static void OnAnnouncingScpTermination(AnnouncingScpTerminationEventArgs ev)
    {
        if (ev.Player?.ReferenceHub is { } hub && Announced.Remove(hub))
            ev.IsAllowed = false;
    }
}
