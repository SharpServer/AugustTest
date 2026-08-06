using System;
using System.Collections.Generic;
using System.IO;
using Exiled.API.Features;
using VoiceChat;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Audio;

/// <summary>
/// 音声ファイルをデコードして覚えておく置き場です。
///
/// <para>
/// ファイル名がそのままクリップ名です。旧実装は「ファイル名」と「クリップ名」を
/// 別々に渡せるようにしていましたが、別名を付けていた呼び出しは 1 件も無く、
/// 「同じファイルを 2 つの名前でロードして片方だけ止める」ための分岐だけが残っていました。
/// </para>
/// </summary>
public static class AudioClips
{
    private static readonly Dictionary<string, float[]> Cache =
        new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 音声ファイルを置くディレクトリです。
    /// </summary>
    public static string Directory => Path.Combine(Paths.Exiled, "ServerContents");

    /// <summary>
    /// クリップを取り出します。まだ読んでいなければデコードします。失敗したら null。
    /// </summary>
    public static float[] Get(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        if (Cache.TryGetValue(fileName, out float[] cached)) return cached;

        string path = Path.Combine(Directory, fileName);

        if (!File.Exists(path))
        {
            Logger.Warn($"[Audio] 音声ファイルがありません: {path}");

            return null;
        }

        float[] samples = Ffmpeg.DecodeAudio(path);

        if (samples is null) return null;

        Cache[fileName] = samples;

        return samples;
    }

    /// <summary>
    /// クリップを先に読み込んでおきます。ラウンド開始直後の詰まりを避けたいときに使います。
    /// </summary>
    public static void Preload(params string[] fileNames)
    {
        foreach (string fileName in fileNames)
        {
            Get(fileName);
        }
    }

    /// <summary>
    /// クリップの長さ (秒) です。読み込めなければ 0。
    /// </summary>
    public static float Duration(string fileName) =>
        Get(fileName) is { } samples ? samples.Length * VoiceChatSettings.SampleToDuartionRate : 0f;

    /// <summary>
    /// サンプル列の長さ (秒) です。
    /// </summary>
    public static float Duration(float[] samples) =>
        samples is null ? 0f : samples.Length * VoiceChatSettings.SampleToDuartionRate;
}
