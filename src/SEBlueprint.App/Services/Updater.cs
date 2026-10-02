using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using SEBlueprint.Core;
using SEBlueprint.Core.Updates;

namespace SEBlueprint.App.Services;

public static class Updater
{
    static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(20);
    static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(15);

    static readonly Lazy<HttpClient> Http = new(() =>
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"SEBlueprintInspector/{CurrentVersion.ToString(3)}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    });

    public static Version CurrentVersion => typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0);

    static string? ExePath => Environment.ProcessPath;

    public static bool CanReplaceExe =>
        ExePath is { } exe
        && exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
        && !File.Exists(Path.ChangeExtension(exe, ".dll"));

    public static async Task<ReleaseInfo?> CheckAsync()
    {
        using var cts = new CancellationTokenSource(CheckTimeout);
        using var response = await Http.Value.GetAsync(ReleaseFeed.LatestApi, cts.Token);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cts.Token);
        var release = ReleaseFeed.Parse(json);
        Log.Write(release == null ? "Update check: no usable release found" : $"Update check: latest is {release.Tag}");
        return release;
    }

    public static async Task InstallAsync(ReleaseInfo release, IProgress<double> progress)
    {
        if (!release.CanInstall || !CanReplaceExe || ExePath is not { } exe)
            throw new InvalidOperationException("This copy can't update itself. Download the new version from the release page.");

        var download = exe + ".download";
        var old = exe + ".old";
        TryDelete(download);
        try
        {
            using (var cts = new CancellationTokenSource(DownloadTimeout))
            using (var response = await Http.Value.GetAsync(release.ExeUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token))
            {
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync(cts.Token);
                await using var target = new FileStream(download, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[1 << 16];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, cts.Token)) > 0)
                {
                    total += read;
                    if (total > release.Size) throw new InvalidDataException("The download is larger than the release says.");
                    hash.AppendData(buffer, 0, read);
                    await target.WriteAsync(buffer.AsMemory(0, read), cts.Token);
                    progress.Report((double)total / release.Size);
                }
                if (total != release.Size) throw new InvalidDataException("The download is incomplete.");
                var actual = Convert.ToHexString(hash.GetHashAndReset());
                if (!ReleaseFeed.ChecksumMatches(actual, release.Sha256!))
                    throw new InvalidDataException("The download doesn't match the release checksum and was discarded.");
            }

            TryDelete(old);
            File.Move(exe, old);
            try { File.Move(download, exe); }
            catch
            {
                File.Move(old, exe);
                throw;
            }
            Log.Write($"Updated to {release.Tag}, restarting");
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe)! });
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("The app's folder is read-only. Download the new version from the release page.");
        }
        finally
        {
            TryDelete(download);
        }
    }

    public static void CleanUpAfterUpdate()
    {
        if (ExePath is not { } exe) return;
        _ = Task.Run(async () =>
        {
            for (var i = 0; i < 10 && File.Exists(exe + ".old"); i++)
            {
                TryDelete(exe + ".old");
                await Task.Delay(1000);
            }
        });
    }

    static void TryDelete(string file)
    {
        try { if (File.Exists(file)) File.Delete(file); }
        catch (Exception ex) { Log.Write($"Could not delete {file}: {ex.Message}"); }
    }
}
