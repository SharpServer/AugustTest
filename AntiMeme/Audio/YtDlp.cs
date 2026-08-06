using System;
using System.IO;
using System.Linq;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Audio;

/// <summary>
/// yt-dlp で Web ページ上のメディアを一時ファイルに落とします。
///
/// <para>
/// ffmpeg が直接読める URL (画像・GIF・動画の直リンク) には使いません。
/// yt-dlp が要るのは YouTube のような「ページから実体を探す」種類の URL だけです。
/// </para>
/// </summary>
public static class YtDlp
{
    /// <summary>
    /// 取得したメディアの置き場です。使い終わったら呼び出し側が消します。
    /// </summary>
    public static string CacheDirectory => Path.Combine(AudioClips.Directory, ".media-cache");

    /// <summary>
    /// yt-dlp に渡せる URL かどうか (絶対 http/https)。
    /// </summary>
    public static bool IsUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// 映像と音声を 1 ファイルに落とします。失敗したら null。
    /// </summary>
    public static string Download(string url) => Download(url, "bestvideo*+bestaudio/best");

    /// <summary>
    /// 音声だけを落とします。失敗したら null。
    /// </summary>
    public static string DownloadAudio(string url) => Download(url, "bestaudio/best");

    /// <summary>
    /// 落としたファイルを削除します。
    /// </summary>
    public static void Discard(string path)
    {
        if (path is not null && path.StartsWith(Path.GetFullPath(CacheDirectory), StringComparison.OrdinalIgnoreCase))
            MediaTools.Delete(path);
    }

    private static string Download(string url, string format)
    {
        if (!IsUrl(url))
        {
            Logger.Warn($"[Audio] yt-dlp に渡せない URL です: {url}");

            return null;
        }

        if (!MediaTools.EnsureYtDlp() || !MediaTools.EnsureFfmpeg()) return null;

        Directory.CreateDirectory(CacheDirectory);

        string template = Path.Combine(CacheDirectory, $"media-{Guid.NewGuid():N}.%(ext)s");

        string output = MediaTools.Run(
            MediaTools.YtDlpPath,
            "--no-config --no-playlist --no-progress --no-warnings " +
            $"--ffmpeg-location \"{MediaTools.Escape(MediaTools.FfmpegPath)}\" " +
            $"-f \"{format}\" -o \"{MediaTools.Escape(template)}\" " +
            $"--print after_move:filepath \"{MediaTools.Escape(url)}\"");

        if (output is null) return null;

        string path = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .LastOrDefault(File.Exists);

        if (path is null)
        {
            Logger.Warn($"[Audio] yt-dlp が取得先のパスを返しませんでした: {url}");

            return null;
        }

        // 出力テンプレートを渡しているのでキャッシュ外に出ることは無いはずだが、
        // 返ってきたパスをそのまま消しに行くので 1 回だけ確かめる。
        string full = Path.GetFullPath(path);

        if (!full.StartsWith(Path.GetFullPath(CacheDirectory), StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warn($"[Audio] yt-dlp がキャッシュ外のパスを返しました: {full}");

            return null;
        }

        return full;
    }
}
