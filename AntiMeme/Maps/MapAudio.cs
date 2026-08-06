using System;
using UnityEngine;

namespace AntiMeme.Maps;

/// <summary>
/// マップ機能から音声層へ渡す細い境界です。購読者がいない場合は安全に無視されます。
/// </summary>
public static class MapAudio
{
    public static event Action<MapAudioRequest> Requested;

    /// <summary>一度だけ鳴らします。</summary>
    public static void Play(string clip, string key, Vector3 position, float volume = 1f, float maxDistance = 10f)
    {
        if (string.IsNullOrWhiteSpace(clip)) return;

        Requested?.Invoke(new MapAudioRequest(clip, key, position, volume, maxDistance, loop: false, stop: false));
    }

    /// <summary>止めるまで繰り返し鳴らします。停止には同じ <paramref name="key"/> を <see cref="Stop"/> へ渡します。</summary>
    public static void Loop(string clip, string key, Vector3 position, float volume = 1f, float maxDistance = 10f)
    {
        if (string.IsNullOrWhiteSpace(clip)) return;

        Requested?.Invoke(new MapAudioRequest(clip, key, position, volume, maxDistance, loop: true, stop: false));
    }

    /// <summary><paramref name="key"/> で鳴らしている音を止めます。</summary>
    public static void Stop(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        Requested?.Invoke(new MapAudioRequest(null, key, Vector3.zero, 0f, 0f, loop: false, stop: true));
    }
}
