using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Exiled.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Audio;

/// <summary>
/// 音声・映像デコードに使う外部実行ファイル (ffmpeg / yt-dlp) を用意します。
///
/// <para>
/// 探す順番は「EXILED の dependencies に既にある」→「PATH にある」→「配布元から取得」です。
/// 取得したものは SHA-256 で検証します。
/// </para>
/// <para>
/// 旧実装は ffmpeg と yt-dlp で PATH 探索・チェックサム検証・実行確認・chmod を
/// まるごと 2 回書いていました。ここでは 1 本にまとめています。
/// </para>
/// </summary>
internal static class MediaTools
{
    private const string FfmpegWindowsUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
    private const string FfmpegLinuxBaseUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/";
    private const string FfmpegLinuxAsset = "ffmpeg-master-latest-linux64-gpl.tar.xz";
    private const string YtDlpBaseUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/";

    private static readonly HttpClient Http = CreateHttpClient();

    private static bool ffmpegReady;
    private static bool ytDlpReady;

    /// <summary>
    /// Windows で動いているかどうか。実行ファイル名と取得方法がここで変わります。
    /// </summary>
    internal static bool IsWindows =>
        Environment.OSVersion.Platform is PlatformID.Win32NT or PlatformID.Win32Windows;

    /// <summary>ffmpeg の実行ファイルパスです。</summary>
    internal static string FfmpegPath => Path.Combine(Paths.Dependencies, IsWindows ? "ffmpeg.exe" : "ffmpeg");

    /// <summary>yt-dlp の実行ファイルパスです。</summary>
    internal static string YtDlpPath => Path.Combine(Paths.Dependencies, IsWindows ? "yt-dlp.exe" : "yt-dlp");

    /// <summary>
    /// ffmpeg を使える状態にします。用意できなければ false を返します。
    /// </summary>
    internal static bool EnsureFfmpeg()
    {
        if (ffmpegReady && File.Exists(FfmpegPath)) return true;

        ffmpegReady = Ensure(FfmpegPath, "ffmpeg", "-version", DownloadFfmpeg);

        return ffmpegReady;
    }

    /// <summary>
    /// yt-dlp を使える状態にします。用意できなければ false を返します。
    /// </summary>
    internal static bool EnsureYtDlp()
    {
        if (ytDlpReady && File.Exists(YtDlpPath)) return true;

        ytDlpReady = Ensure(YtDlpPath, "yt-dlp", "--no-config --version", DownloadYtDlp);

        return ytDlpReady;
    }

    /// <summary>
    /// 外部プロセスを実行し、標準出力を返します。失敗したら null。
    /// </summary>
    internal static string Run(string executable, string arguments, int timeoutMs = 600000)
    {
        try
        {
            using Process process = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            });

            if (process is null) return null;

            // 先に非同期で吸い出さないと、出力バッファが埋まった時点で子プロセスが止まる。
            System.Threading.Tasks.Task<string> output = process.StandardOutput.ReadToEndAsync();
            System.Threading.Tasks.Task<string> error = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeoutMs))
            {
                process.Kill();
                Logger.Warn($"[Audio] {Path.GetFileName(executable)} が時間内に終わりませんでした。");

                return null;
            }

            if (process.ExitCode == 0) return output.GetAwaiter().GetResult();

            Logger.Warn($"[Audio] {Path.GetFileName(executable)} が失敗しました (exit {process.ExitCode}): {error.GetAwaiter().GetResult().Trim()}");

            return null;
        }
        catch (Exception exception)
        {
            Logger.Warn($"[Audio] {Path.GetFileName(executable)} を実行できませんでした: {exception.Message}");

            return null;
        }
    }

    /// <summary>
    /// 実行ファイルの引数に埋め込めるようにエスケープします。
    /// </summary>
    internal static string Escape(string value) => value.Replace("\"", "\\\"");

    /// <summary>
    /// 指定パスに実行ファイルを揃えます。
    /// </summary>
    private static bool Ensure(string path, string name, string versionArguments, Action<string> download)
    {
        try
        {
            Directory.CreateDirectory(Paths.Dependencies);

            if (File.Exists(path))
            {
                MakeExecutable(path);

                if (Run(path, versionArguments, timeoutMs: 30000) is not null) return true;

                Logger.Warn($"[Audio] 既存の {name} が起動できないので入れ直します: {path}");
                File.Delete(path);
            }

            if (FindOnPath(IsWindows ? name + ".exe" : name) is { } onPath)
            {
                File.Copy(onPath, path, overwrite: true);
            }
            else
            {
                Logger.Info($"[Audio] {name} が見つからないので {Paths.Dependencies} に取得します。");
                download(path);
            }

            MakeExecutable(path);

            if (Run(path, versionArguments, timeoutMs: 30000) is not null)
            {
                Logger.Info($"[Audio] {name} を用意しました: {path}");

                return true;
            }

            Delete(path);
            Logger.Warn($"[Audio] 取得した {name} を起動できませんでした。");

            return false;
        }
        catch (Exception exception)
        {
            Logger.Warn($"[Audio] {name} を用意できませんでした: {exception.Message}");

            return false;
        }
    }

    private static void DownloadYtDlp(string destination)
    {
        string asset = IsWindows ? "yt-dlp.exe" : "yt-dlp_linux";
        string temporary = destination + ".download";

        try
        {
            DownloadFile(YtDlpBaseUrl + asset, temporary);
            VerifySha256(temporary, FindChecksum(DownloadString(YtDlpBaseUrl + "SHA2-256SUMS"), asset));
            File.Copy(temporary, destination, overwrite: true);
        }
        finally
        {
            Delete(temporary);
        }
    }

    private static void DownloadFfmpeg(string destination)
    {
        if (IsWindows)
        {
            DownloadFfmpegZip(destination);

            return;
        }

        if (Environment.OSVersion.Platform is not PlatformID.Unix)
            throw new PlatformNotSupportedException($"{Environment.OSVersion.Platform} への ffmpeg 自動導入には対応していません。");

        DownloadFfmpegTarXz(destination);
    }

    private static void DownloadFfmpegZip(string destination)
    {
        string archive = Path.Combine(Paths.Dependencies, $"ffmpeg-{Guid.NewGuid():N}.zip");

        try
        {
            string expected = DownloadString(FfmpegWindowsUrl + ".sha256").Trim();
            DownloadFile(FfmpegWindowsUrl, archive);
            VerifySha256(archive, expected);

            using FileStream stream = File.OpenRead(archive);
            using ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read);

            ZipArchiveEntry entry = zip.Entries.FirstOrDefault(candidate =>
                candidate.FullName.Replace('\\', '/').EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                throw new InvalidDataException("取得した ffmpeg アーカイブに bin/ffmpeg.exe がありません。");

            using Stream input = entry.Open();
            using FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

            input.CopyTo(output);
        }
        finally
        {
            Delete(archive);
        }
    }

    private static void DownloadFfmpegTarXz(string destination)
    {
        string archive = Path.Combine(Paths.Dependencies, $"ffmpeg-{Guid.NewGuid():N}.tar.xz");
        string extracted = Path.Combine(Paths.Dependencies, $"ffmpeg-{Guid.NewGuid():N}");

        try
        {
            string checksums = DownloadString(FfmpegLinuxBaseUrl + "checksums.sha256");
            DownloadFile(FfmpegLinuxBaseUrl + FfmpegLinuxAsset, archive);
            VerifySha256(archive, FindChecksum(checksums, FfmpegLinuxAsset));

            Directory.CreateDirectory(extracted);

            string tar = FindOnPath("tar") ?? "/bin/tar";

            if (Run(tar, $"-xJf \"{Escape(archive)}\" -C \"{Escape(extracted)}\"") is null)
                throw new InvalidOperationException("tar による ffmpeg の展開に失敗しました。");

            string binary = Directory.EnumerateFiles(extracted, "ffmpeg", SearchOption.AllDirectories).FirstOrDefault();

            if (binary is null)
                throw new InvalidDataException("取得した ffmpeg アーカイブに実行ファイルがありません。");

            File.Copy(binary, destination, overwrite: true);
        }
        finally
        {
            Delete(archive);

            try
            {
                if (Directory.Exists(extracted))
                    Directory.Delete(extracted, recursive: true);
            }
            catch (Exception exception)
            {
                Logger.Debug($"[Audio] 一時ディレクトリを消せませんでした: {exception.Message}");
            }
        }
    }

    private static string FindOnPath(string executable)
    {
        string paths = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(paths)) return null;

        foreach (string directory in paths.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory)) continue;

            try
            {
                string candidate = Path.Combine(directory.Trim().Trim('"'), executable);

                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException)
            {
                // PATH に不正な文字が混ざっている項目は読み飛ばす。
            }
        }

        return null;
    }

    private static void MakeExecutable(string path)
    {
        if (IsWindows) return;

        Run("/bin/chmod", $"+x \"{Escape(path)}\"", timeoutMs: 10000);
    }

    private static string FindChecksum(string manifest, string asset)
    {
        foreach (string line in manifest.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2 && parts[1].TrimStart('*') == asset) return parts[0];
        }

        throw new InvalidDataException($"チェックサム一覧に {asset} がありません。");
    }

    private static void VerifySha256(string path, string expected)
    {
        if (expected.Length != 64 || expected.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException("配布元が返した SHA-256 が不正です。");

        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(path);

        string actual = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);

        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("取得したファイルの SHA-256 が一致しません。");
    }

    private static string DownloadString(string url)
    {
        using HttpResponseMessage response = Http.GetAsync(url).GetAwaiter().GetResult();

        response.EnsureSuccessStatusCode();

        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    private static void DownloadFile(string url, string destination)
    {
        using HttpResponseMessage response = Http
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
            .GetAwaiter()
            .GetResult();

        response.EnsureSuccessStatusCode();

        using Stream input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

        input.CopyTo(output);
    }

    internal static void Delete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception)
        {
            Logger.Debug($"[Audio] 一時ファイルを消せませんでした ({path}): {exception.Message}");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        HttpClient client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        })
        {
            Timeout = TimeSpan.FromMinutes(15),
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("AntiMeme-MediaTools/1.0");

        return client;
    }
}
