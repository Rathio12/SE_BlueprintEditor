using System.Text.RegularExpressions;

namespace SEBlueprint.Core.Tests;

public class OfflineGuardTests
{
    const string Allowed = "https://github.com/Rathio12/SE_BlueprintEditor";
    const string Site = "https://rathio12.github.io/SE_BlueprintEditor/";
    static readonly string[] Banned = { "HttpClient", "WebClient", "WebRequest", "DllImport", "LibraryImport", "TcpClient", "UdpClient", "new Socket(", "WebSocket", "fetch(", "XMLHttpRequest" };
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

    [Fact]
    public void Source_has_no_network_native_or_foreign_urls()
    {
        var bad = new List<string>();
        foreach (var f in SourceFiles())
        {
            var t = File.ReadAllText(f);
            foreach (Match m in Regex.Matches(t, "https?://[^\\s\"'<>)]+"))
            {
                var u = m.Value;
                if (u.StartsWith(Allowed) || u.StartsWith(Site) || u == "https://schema.org" || u.StartsWith("http://schemas.") || u.StartsWith("http://www.w3.org/")) continue;
                bad.Add($"{f}: {u}");
            }
            foreach (var w in Banned)
                if (t.Contains(w)) bad.Add($"{f}: {w}");
        }
        Assert.Empty(bad);
    }

    [Fact]
    public void Guard_actually_scans_source()
    {
        Assert.Contains(SourceFiles(), f => f.EndsWith("BlueprintAnalyzer.cs"));
    }
}
