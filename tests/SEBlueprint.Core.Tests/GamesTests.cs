using SEBlueprint.Core.Games;
using SEBlueprint.Core.Paths;

namespace SEBlueprint.Core.Tests;

public class GamesTests
{
    [Fact]
    public void Se2_reads_metadata_and_isolates_broken()
    {
        var ok = Se2Adapter.ReadEntry(TestData.Fx("se2", "MyShip"));
        Assert.Equal("My Ship", ok.Name);
        Assert.Equal("fast", ok.Description);
        Assert.Equal(GameId.SE2, ok.Game);
        Assert.Null(ok.Error);
        Assert.NotNull(ok.Report);
        Assert.True(ok.Report!.IsPartial);
        Assert.Equal(760, ok.Report.Blocks);
        Assert.Equal(2897, ok.Report.Pcu);

        var bad = Se2Adapter.ReadEntry(TestData.Fx("se2", "Broken"));
        Assert.Equal("Broken", bad.Name);
        Assert.NotNull(bad.Error);
        Assert.Null(bad.Report);
    }

    [Fact]
    public void Se2_adapter_enumerates_folder()
    {
        var p = new GamePaths(null, null, Array.Empty<BlueprintRoot>()) { Se2BlueprintDir = TestData.Fx("se2") };
        var a = new Se2Adapter(p);
        Assert.False(a.CanAnalyze);
        Assert.Equal(2, a.Enumerate().Count);
        Assert.False(new Se2Adapter(new GamePaths(null, null, Array.Empty<BlueprintRoot>())).IsInstalled);
        Assert.Empty(new Se2Adapter(new GamePaths(null, null, Array.Empty<BlueprintRoot>())).Enumerate());
    }

    [Fact]
    public void Se1_adapter_enumerates_roots_as_se1()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        File.Copy(TestData.Fx("bp_test", "bp.sbc"), Path.Combine(Directory.CreateDirectory(Path.Combine(root, "A")).FullName, "bp.sbc"));
        var a = new Se1Adapter(new GamePaths(root, null, new[] { new BlueprintRoot(root, "Local") }));
        Assert.True(a.CanAnalyze);
        Assert.True(a.IsInstalled);
        Assert.Equal(GameId.SE1, Assert.Single(a.Enumerate()).Game);
    }

    [Fact]
    public void Build_detects_se2_install_and_blueprints()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var lib = Path.Combine(root, "lib");
        var app = Path.Combine(root, "app");
        Directory.CreateDirectory(Path.Combine(lib, "steamapps/common/SpaceEngineers2"));
        Directory.CreateDirectory(Path.Combine(app, "SpaceEngineers2/AppData/Blueprints"));
        var p = SteamLocator.Build(new[] { lib }, app);
        Assert.EndsWith("SpaceEngineers2", p.Se2GameDir);
        Assert.EndsWith("Blueprints", p.Se2BlueprintDir);
        Assert.Null(p.GameDir);
    }
}
