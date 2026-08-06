using AntiMeme.Audio;
using LabApi.Events.Handlers;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using ExiledRoom = Exiled.API.Features.Room;
using RoomType = Exiled.API.Enums.RoomType;

namespace AntiMeme.Changes;

/// <summary>
/// 隠し要素です。核格納庫の片隅で、気づいた人だけに聞こえる曲が流れ続けます。
/// </summary>
public sealed class EasterEggs : EventHandlerBase
{
    private const string MelancholyClip = "ee_melancholy.ogg";
    private const string MelancholySpeaker = "EE_Melancholy";

    /// <summary>音が届く距離です。真横に立たないと聞こえません。</summary>
    private const float Range = 6f;

    private static readonly Vector3 Offset = new Vector3(-2.25f, -5.65f, 0f);

    /// <inheritdoc/>
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        SpeakerApi.LoadClip(MelancholyClip);
        ServerEvents.RoundStarted += PlayMelancholy;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        ServerEvents.RoundStarted -= PlayMelancholy;
        SpeakerApi.TryDestroy(MelancholySpeaker);
    }

    /// <inheritdoc/>
    public override void OnServerRoundRestarted() => SpeakerApi.TryDestroy(MelancholySpeaker);

    private static void PlayMelancholy()
    {
        if (ExiledRoom.Get(RoomType.HczNuke) is not { } room) return;

        SpeakerApi.PlayLoop(
            MelancholyClip,
            MelancholySpeaker,
            room.Position + room.Rotation * Offset,
            isSpatial: true,
            maxDistance: Range,
            minDistance: 0f);
    }
}
