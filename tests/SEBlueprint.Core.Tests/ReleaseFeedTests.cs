using SEBlueprint.Core.Updates;

namespace SEBlueprint.Core.Tests;

public class ReleaseFeedTests
{
    const string Digest = "d331d54642c7beeedcd7eab7a9aa2416cf7fa9e16130b01c60e94d0141afb87a";

    static string Json(string tag = "v1.2.0", string? exeUrl = null, string? digest = "sha256:" + Digest,
        string name = "SEBlueprintInspector.exe", bool draft = false, bool prerelease = false,
        string page = "https://github.com/Rathio12/SE_BlueprintEditor/releases/tag/v1.2.0")
    {
        exeUrl ??= $"https://github.com/Rathio12/SE_BlueprintEditor/releases/download/{tag}/SEBlueprintInspector.exe";
        var d = digest == null ? "null" : $"\"{digest}\"";
        return $$"""
        {
          "tag_name": "{{tag}}",
          "html_url": "{{page}}",
          "draft": {{draft.ToString().ToLowerInvariant()}},
          "prerelease": {{prerelease.ToString().ToLowerInvariant()}},
          "body": "### Fixed\n- Something",
          "assets": [
            { "name": "{{name}}", "size": 66261786, "digest": {{d}}, "browser_download_url": "{{exeUrl}}" }
          ]
        }
        """;
    }

    [Fact]
    public void Reads_version_notes_exe_and_checksum()
    {
        var r = ReleaseFeed.Parse(Json());
        Assert.NotNull(r);
        Assert.Equal(new Version(1, 2, 0), r!.Version);
        Assert.Equal("v1.2.0", r.Tag);
        Assert.Contains("Something", r.Notes);
        Assert.Equal("https://github.com/Rathio12/SE_BlueprintEditor/releases/download/v1.2.0/SEBlueprintInspector.exe", r.ExeUrl);
        Assert.Equal(Digest, r.Sha256);
        Assert.Equal(66261786, r.Size);
        Assert.True(r.CanInstall);
    }

    [Theory]
    [InlineData("1.1.1", true)]
    [InlineData("1.2.0", false)]
    [InlineData("1.2.0.0", false)]
    [InlineData("1.3.0", false)]
    public void Detects_newer_versions(string current, bool expected)
    {
        var r = ReleaseFeed.Parse(Json())!;
        Assert.Equal(expected, ReleaseFeed.IsNewer(r, Version.Parse(current)));
    }

    [Fact]
    public void Ignores_drafts_prereleases_and_bad_tags()
    {
        Assert.Null(ReleaseFeed.Parse(Json(draft: true)));
        Assert.Null(ReleaseFeed.Parse(Json(prerelease: true)));
        Assert.Null(ReleaseFeed.Parse(Json(tag: "nightly")));
        Assert.Null(ReleaseFeed.Parse("not json"));
        Assert.Null(ReleaseFeed.Parse(""));
    }

    [Fact]
    public void Refuses_to_install_from_anywhere_but_this_repository()
    {
        var r = ReleaseFeed.Parse(Json(exeUrl: "https://example.org/SEBlueprintInspector.exe"));
        Assert.NotNull(r);
        Assert.Null(r!.ExeUrl);
        Assert.False(r.CanInstall);
    }

    [Fact]
    public void Release_page_from_another_repository_falls_back_to_the_releases_page()
    {
        var r = ReleaseFeed.Parse(Json(page: "https://example.org/fake"))!;
        Assert.Equal(ReleaseFeed.ReleasesPage, r.PageUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("md5:abc")]
    [InlineData("sha256:1234")]
    public void Without_a_valid_checksum_the_update_is_not_installed(string? digest)
    {
        var r = ReleaseFeed.Parse(Json(digest: digest))!;
        Assert.Null(r.Sha256);
        Assert.False(r.CanInstall);
    }

    [Fact]
    public void Without_the_exe_asset_the_update_is_not_installed()
    {
        var r = ReleaseFeed.Parse(Json(name: "Source.zip"))!;
        Assert.False(r.CanInstall);
    }

    [Fact]
    public void Checksum_comparison_ignores_case_only()
    {
        Assert.True(ReleaseFeed.ChecksumMatches(Digest.ToUpperInvariant(), Digest));
        Assert.False(ReleaseFeed.ChecksumMatches(Digest, Digest[..^1] + "0"));
    }
}
