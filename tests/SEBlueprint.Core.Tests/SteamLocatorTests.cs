using SEBlueprint.Core.Paths;

namespace SEBlueprint.Core.Tests;

public class SteamLocatorTests
{
    const string Vdf = "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"path\"\t\t\"C:\\Program Files (x86)\\Steam\"\n\t}\n\t\"1\"\n\t{\n\t\t\"path\"\t\t\"D:\\SteamLibrary\"\n\t}\n}";

    [Fact]
    public void Parses_paths_and_unescapes() =>
        Assert.Equal(new[] { @"C:\Program Files (x86)\Steam", @"D:\SteamLibrary" }, SteamLocator.ParseLibraryFolders(Vdf));

    [Fact]
    public void Build_finds_game_workshop_and_blueprints()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var lib = Path.Combine(root, "lib");
        var app = Path.Combine(root, "app");
        Directory.CreateDirectory(Path.Combine(lib, "steamapps/common/SpaceEngineers"));
        Directory.CreateDirectory(Path.Combine(lib, "steamapps/workshop/content/244850"));
        Directory.CreateDirectory(Path.Combine(app, "SpaceEngineers/Blueprints/local"));
        var p = SteamLocator.Build(new[] { Path.Combine(root, "missing"), lib }, app);
        Assert.EndsWith("SpaceEngineers", p.GameDir);
        Assert.EndsWith("244850", p.WorkshopDir);
        Assert.Contains(p.BlueprintRoots, r => r.Source == "Local");
        Assert.Contains(p.BlueprintRoots, r => r.Source == "Workshop");
    }

    [Fact]
    public void Build_with_nothing_returns_nulls()
    {
        var p = SteamLocator.Build(Array.Empty<string>(), Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Assert.Null(p.GameDir);
        Assert.Null(p.WorkshopDir);
        Assert.Empty(p.BlueprintRoots);
    }
}
