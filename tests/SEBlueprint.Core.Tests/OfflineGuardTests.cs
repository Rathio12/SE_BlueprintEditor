using System.Text.RegularExpressions;

namespace SEBlueprint.Core.Tests;

public class OfflineGuardTests
{
    const string Allowed = "https://github.com/Rathio12/SE_BlueprintEditor";
    const string Site = "https://rathio12.github.io/SE_BlueprintEditor/";
    const string UpdateApi = "https://api.github.com/repos/Rathio12/SE_BlueprintEditor/releases/latest";
    static readonly string[] Network = { "HttpClient", "WebClient", "WebRequest", "TcpClient", "UdpClient", "new Socket(", "WebSocket", "fetch(", "XMLHttpRequest" };
    static readonly string[] Native = { "DllImport", "LibraryImport" };
    static readonly string UpdaterFile = Path.Combine("src", "SEBlueprint.App", "Services", "Updater.cs");
    static readonly string FeedFile = Path.Combine("src", "SEBlueprint.Core", "Updates", "ReleaseFeed.cs");
    static readonly string[] Extensions = { ".cs", ".xaml", ".razor", ".html", ".css", ".js", ".json", ".webmanifest" };

    static string RepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !d.EnumerateFiles("SEBlueprintInspector.sln*").Any()) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("Repository root not found");
    }

    static IEnumerable<string> SourceFiles()
    {
        var sep = Path.DirectorySeparatorChar;
        string[] skip = { $"{sep}obj{sep}", $"{sep}bin{sep}", $"{sep}_framework{sep}", $"{sep}data{sep}" };
        return Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.*", SearchOption.AllDirectories)
            .Where(f => Extensions.Contains(Path.GetExtension(f)) && !skip.Any(f.Contains));
    }

    static bool Is(string file, string relative) => file.EndsWith(Path.DirectorySeparatorChar + relative, StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Source_has_no_network_native_or_foreign_urls()
    {
        var bad = new List<string>();
        foreach (var f in SourceFiles())
        {
            var t = File.ReadAllText(f);
            var isUpdater = Is(f, UpdaterFile);
            foreach (Match m in Regex.Matches(t, "https?://[^\\s\"'<>)]+"))
            {
                var u = m.Value;
                if (u == UpdateApi && Is(f, FeedFile)) continue;
                if (u.StartsWith(Allowed) || u.StartsWith(Site) || u == "https://schema.org" || u.StartsWith("http://schemas.") || u.StartsWith("http://www.w3.org/")) continue;
                bad.Add($"{f}: {u}");
            }
            foreach (var w in Native)
                if (t.Contains(w)) bad.Add($"{f}: {w}");
            if (!isUpdater)
                foreach (var w in Network)
                    if (t.Contains(w)) bad.Add($"{f}: {w}");
        }
        Assert.Empty(bad);
    }

    [Fact]
    public void Updater_only_talks_to_this_repository_and_only_when_asked()
    {
        var f = SourceFiles().Single(x => Is(x, UpdaterFile));
        var t = File.ReadAllText(f);
        Assert.DoesNotMatch("https?://", t);
        Assert.Contains("ReleaseFeed.LatestApi", t);
        Assert.Contains("ReleaseFeed.ChecksumMatches", t);
        var settings = File.ReadAllText(SourceFiles().Single(x => Is(x, Path.Combine("src", "SEBlueprint.App", "Services", "AppSettings.cs"))));
        Assert.Matches(@"bool CheckUpdatesOnStart \{ get; set; \}\r?\n", settings);
    }

    [Fact]
    public void Guard_actually_scans_source()
    {
        Assert.Contains(SourceFiles(), f => f.EndsWith("BlueprintAnalyzer.cs"));
    }
}
