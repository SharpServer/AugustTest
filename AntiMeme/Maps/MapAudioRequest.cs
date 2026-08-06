using UnityEngine;

namespace AntiMeme.Maps;

/// <summary>音声ドメインを直接参照しないための要求イベントです。</summary>
public readonly struct MapAudioRequest
{
    public MapAudioRequest(string clip, string key, Vector3 position, float volume, float maxDistance, bool loop, bool stop)
    {
        Clip = clip ?? string.Empty;
        Key = key ?? string.Empty;
        Position = position;
        Volume = volume;
        MaxDistance = maxDistance;
        Loop = loop;
        Stop = stop;
    }

    public string Clip { get; }

    public string Key { get; }

    public Vector3 Position { get; }

    public float Volume { get; }

    public float MaxDistance { get; }

    /// <summary>繰り返し再生するか。</summary>
    public bool Loop { get; }

    /// <summary>再生ではなく、<see cref="Key"/> の音を止める要求か。</summary>
    public bool Stop { get; }
}
