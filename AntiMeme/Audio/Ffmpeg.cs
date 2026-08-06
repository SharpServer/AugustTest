using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using VoiceChat;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Audio;

/// <summary>
/// ffmpeg で音声と映像をデコードします。
///
/// <para>
/// 出力先は 2 つだけです。音声はゲームのボイスチャットが要求する 48 kHz モノラル float PCM、
/// 映像は 1 ピクセル 1 バイトのグレースケールです。それ以外の形式は要りません。
/// </para>
/// <para>
/// 外部プロセスなのでここは <c>try</c>/<c>catch</c> を書いてよい数少ない場所です。
/// 失敗したら例外ではなく null / 空を返し、呼び出し側は「鳴らない」だけで済むようにします。
/// </para>
/// </summary>
public static class Ffmpeg
{
    /// <summary>1 回の映像デコードで確保してよいバイト数の上限です。</summary>
    private const long MaxFrameBytes = 512L * 1024L * 1024L;

    /// <summary>
    /// 音声ファイルを 48 kHz モノラルの float PCM に変換します。失敗したら null。
    /// </summary>
    public static float[] DecodeAudio(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return null;
        if (!MediaTools.EnsureFfmpeg()) return null;

        string arguments =
            $"-v error -nostdin -i \"{MediaTools.Escape(source)}\" -map 0:a:0 -vn " +
            $"-ac 1 -ar {VoiceChatSettings.SampleRate} -f f32le pipe:1";

        byte[] raw = ReadPipe(arguments, source);

        if (raw is null || raw.Length < sizeof(float) || raw.Length % sizeof(float) != 0)
        {
            Logger.Warn($"[Audio] 音声をデコードできませんでした: {source}");

            return null;
        }

        float[] samples = new float[raw.Length / sizeof(float)];
        Buffer.BlockCopy(raw, 0, samples, 0, raw.Length);

        return samples;
    }

    /// <summary>
    /// 映像 (静止画・GIF・動画・直リンク URL) を <paramref name="width"/> x <paramref name="height"/> の
    /// グレースケールフレーム列に変換します。失敗したら空。
    /// </summary>
    /// <param name="source">ファイルパスまたは直リンク URL。</param>
    /// <param name="width">出力幅。</param>
    /// <param name="height">出力高さ。</param>
    /// <param name="framesPerSecond">サンプリングするフレームレート。</param>
    /// <param name="maxFrames">取り出すフレーム数の上限。</param>
    public static IReadOnlyList<byte[]> DecodeFrames(
        string source,
        int width,
        int height,
        float framesPerSecond,
        int maxFrames)
    {
        if (string.IsNullOrWhiteSpace(source)) return [];
        if (width < 1 || height < 1 || framesPerSecond <= 0f || maxFrames < 1) return [];
        if (!MediaTools.EnsureFfmpeg()) return [];

        int frameSize = width * height;

        if ((long)frameSize * maxFrames > MaxFrameBytes)
        {
            Logger.Warn($"[Audio] 要求されたフレーム数がメモリ上限を超えます: {source}");

            return [];
        }

        string fps = framesPerSecond.ToString("0.########", CultureInfo.InvariantCulture);

        // 静止画には尺が無い。eof_action=pass を付けないと fps フィルタが唯一のフレームを
        // 捨てたまま正常終了し、出力が空になる。
        string filter =
            $"fps={fps}:eof_action=pass," +
            $"scale={width}:{height}:force_original_aspect_ratio=decrease," +
            $"pad={width}:{height}:(ow-iw)/2:(oh-ih)/2:color=black";

        string arguments =
            $"-v error -nostdin -i \"{MediaTools.Escape(source)}\" -map 0:v:0 -an " +
            $"-vf \"{filter}\" -frames:v {maxFrames} -pix_fmt gray -f rawvideo pipe:1";

        byte[] raw = ReadPipe(arguments, source);

        if (raw is null || raw.Length < frameSize)
        {
            Logger.Warn($"[Audio] 映像をデコードできませんでした: {source}");

            return [];
        }

        List<byte[]> frames = new List<byte[]>(raw.Length / frameSize);

        for (int offset = 0; offset + frameSize <= raw.Length; offset += frameSize)
        {
            byte[] frame = new byte[frameSize];
            Buffer.BlockCopy(raw, offset, frame, 0, frameSize);
            frames.Add(frame);
        }

        return frames;
    }

    /// <summary>
    /// ffmpeg を起動し、標準出力のバイト列をすべて読み取ります。失敗したら null。
    /// </summary>
    private static byte[] ReadPipe(string arguments, string source)
    {
        StringBuilder errors = new StringBuilder();

        try
        {
            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = MediaTools.FfmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    errors.AppendLine(args.Data);
            };

            if (!process.Start()) return null;

            process.BeginErrorReadLine();

            using MemoryStream output = new MemoryStream();

            process.StandardOutput.BaseStream.CopyTo(output);
            process.WaitForExit();

            if (process.ExitCode == 0) return output.ToArray();

            Logger.Warn($"[Audio] ffmpeg が失敗しました ({source}): {errors.ToString().Trim()}");

            return null;
        }
        catch (Exception exception)
        {
            Logger.Warn($"[Audio] ffmpeg を実行できませんでした ({source}): {exception.Message}");

            return null;
        }
    }
}
