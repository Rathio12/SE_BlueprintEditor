using System.Text.Json;
using System.Text.RegularExpressions;

namespace SEBlueprint.Core.Updates;

public sealed record ReleaseInfo(Version Version, string Tag, string Notes, string PageUrl, string? ExeUrl, string? Sha256, long Size)
{
    public bool CanInstall => ExeUrl != null && Sha256 != null && Size > 0;
}

public static class ReleaseFeed
{
    public const string Repo = "https://github.com/Rathio12/SE_BlueprintEditor";
    public const string ReleasesPage = Repo + "/releases";
    public const string LatestApi = "https://api.github.com/repos/Rathio12/SE_BlueprintEditor/releases/latest";
    public const string ExeName = "SEBlueprintInspector.exe";

    const string DownloadPrefix = Repo + "/releases/download/";
    static readonly Regex Sha256Hex = new("^[0-9a-fA-F]{64}$", RegexOptions.CultureInvariant);

    public static ReleaseInfo? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (Bool(root, "draft") || Bool(root, "prerelease")) return null;

            var tag = Str(root, "tag_name") ?? "";
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var parsed)) return null;
            var version = Normalize(parsed);

            var page = Str(root, "html_url");
            if (page == null || !page.StartsWith(ReleasesPage + "/", StringComparison.Ordinal)) page = ReleasesPage;

            string? exeUrl = null, sha = null;
            long size = 0;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    if (!string.Equals(Str(a, "name"), ExeName, StringComparison.OrdinalIgnoreCase)) continue;
                    var url = Str(a, "browser_download_url");
                    if (url != null && url.StartsWith(DownloadPrefix, StringComparison.Ordinal) && url.EndsWith("/" + ExeName, StringComparison.Ordinal))
                        exeUrl = url;
                    var digest = Str(a, "digest");
                    if (digest != null && digest.StartsWith("sha256:", StringComparison.Ordinal) && Sha256Hex.IsMatch(digest[7..]))
                        sha = digest[7..].ToLowerInvariant();
                    if (a.TryGetProperty("size", out var s) && s.ValueKind == JsonValueKind.Number) size = s.GetInt64();
                    break;
                }
            }

            return new ReleaseInfo(version, tag, Str(root, "body") ?? "", page, exeUrl, sha, size);
        }
        catch (JsonException) { return null; }
    }

    public static bool IsNewer(ReleaseInfo release, Version current) => release.Version > Normalize(current);

    public static bool ChecksumMatches(string actualHex, string expectedHex) =>
        string.Equals(actualHex, expectedHex, StringComparison.OrdinalIgnoreCase);

    static Version Normalize(Version v) => new(v.Major, Math.Max(0, v.Minor), Math.Max(0, v.Build));

    static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    static bool Bool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.True;
}
