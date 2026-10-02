using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Blueprints;
using SEBlueprint.Core.Data;

namespace SEBlueprint.Core.Tests;

public class AnalyzerTests
{
    static BlueprintData TestBp() => BlueprintData.Load(TestData.Fx("bp_test", "bp.sbc"));

    [Fact]
    public void Parses_blueprint_structure()
    {
        var bp = TestBp();
        Assert.Equal("Test Ship", bp.Name);
        var grid = Assert.Single(bp.Grids);
        Assert.Equal("Large", grid.GridSize);
        Assert.Equal("Test Ship Main", grid.Name);
        Assert.Equal(8, grid.Blocks.Count);
        Assert.Equal("LargeGatlingTurret/", grid.Blocks[0].Id);
        Assert.Equal("Backward", grid.Blocks[4].Forward);
        Assert.Equal("Forward", grid.Blocks[1].Forward);
        Assert.Equal(new[] { "1919062467", "42" }, bp.Mods.Select(m => m.Id));
        Assert.Equal("Swarm", bp.Mods[0].Name);
    }

    [Fact]
    public void Totals_guns_cargo_unknowns()
    {
        var (data, ws) = TestData.Fake();
        var db = GameDatabaseLoader.Load(data, ws);
        var r = BlueprintAnalyzer.Analyze(TestBp(), db);
        Assert.Equal(8, r.Blocks);
        Assert.True(r.IsIncomplete);
        Assert.Equal(225 * 2 + 80 + 10 + 15 * 2 + 100, r.Pcu);
        Assert.Equal(2, r.Turrets);
        Assert.Equal(2, r.FixedWeapons);
        Assert.Equal(4, r.Guns);
        Assert.Equal(1, r.CargoContainers);
        Assert.Equal(8000, r.CargoLiters, 3);
        Assert.Equal(2, r.Thrusters);
        Assert.Equal(2 * 4320000, r.MaxThrustN, 0);
        Assert.Equal(1, r.UnknownBlocks["Unknown/ModBlock"]);
        Assert.True(r.UsesMods);
        Assert.Contains("42", r.MissingMods);
        Assert.DoesNotContain("1919062467", r.MissingMods);
        Assert.Equal(15 * 2 + 4 + 360 + 25 * 2 + 50, r.Components["Component/SteelPlate"]);
        Assert.Equal(r.Components["Component/SteelPlate"] * 20.0, r.MassKg, 3);
        Assert.Equal(8, r.Grids.Single().Blocks);
        Assert.Equal(1, r.BlockPairCounts["Unknown"]);
        Assert.Equal(2, r.BlockPairCounts["LargeGatlingTurret"]);
        Assert.Equal(1, r.BlockPairCounts["LargeContainer"]);
        Assert.True(r.Ingots["Ingot/Iron"] > 0);
        Assert.True(r.Ore["Ore/Iron"] > 0);
    }

    [Fact]
    public void Analyzes_with_empty_database_without_crashing()
    {
        var r = BlueprintAnalyzer.Analyze(TestBp(), new GameDatabase());
        Assert.Equal(8, r.Blocks);
        Assert.Equal(8, r.UnknownBlocks.Values.Sum());
        Assert.True(r.IsIncomplete);
        Assert.Empty(r.Ingots);
    }

    [Fact]
    public void Jump_range_scales_with_mass()
    {
        var db = new GameDatabase();
        db.Blocks["JumpDrive/J"] = new BlockDefinition { Id = "JumpDrive/J", Category = BlockCategory.JumpDrive, JumpDistance = 2000, JumpMaxMass = 1000, Components = { ["Component/Heavy"] = 1 } };
        db.Items["Component/Heavy"] = new ComponentDefinition { Id = "Component/Heavy", Mass = 4000 };
        var bp = new BlueprintData("x", "", "", new[] { new GridData("g", "Large", new[] { new BlockRef("JumpDrive/J", "Forward") }) }, Array.Empty<BlueprintMod>());
        var r = BlueprintAnalyzer.Analyze(bp, db);
        Assert.Equal(1, r.JumpDrives);
        Assert.Equal(500, r.JumpRangeKm, 3);
    }

    [Fact]
    public void Cost_expands_to_ingots_and_ore()
    {
        var (data, ws) = TestData.Fake();
        var db = GameDatabaseLoader.Load(data, ws);
        var plates = new Dictionary<string, int> { ["Component/SteelPlate"] = 10 };
        var (ingots, ore) = new CostCalculator().Compute(plates, db, Array.Empty<string>());
        Assert.Equal(210, ingots["Ingot/Iron"], 6);
        Assert.Equal(300, ore["Ore/Iron"], 6);
        var (i3, _) = new CostCalculator { AssemblerEfficiency = 3 }.Compute(plates, db, Array.Empty<string>());
        Assert.Equal(70, i3["Ingot/Iron"], 6);
        var (_, o2) = new CostCalculator { YieldMultiplier = 2 }.Compute(plates, db, Array.Empty<string>());
        Assert.Equal(150, o2["Ore/Iron"], 6);
    }

    [Fact]
    public void Cost_handles_components_made_from_components_and_cycles()
    {
        var db = new GameDatabase();
        db.Recipes["Component/A"] = new() { ["Component/B"] = 2 };
        db.Recipes["Component/B"] = new() { ["Ingot/Iron"] = 3 };
        db.Recipes["Component/X"] = new() { ["Component/Y"] = 1 };
        db.Recipes["Component/Y"] = new() { ["Component/X"] = 1 };
        var (ingots, _) = new CostCalculator().Compute(new Dictionary<string, int> { ["Component/A"] = 1, ["Component/X"] = 1 }, db, Array.Empty<string>());
        Assert.Equal(6, ingots["Ingot/Iron"], 6);
    }

    [Fact]
    public void Reads_gzip_compressed_blueprints()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        var file = Path.Combine(dir, "bp.sbc");
        using (var output = File.Create(file))
        using (var gz = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
        using (var input = File.OpenRead(TestData.Fx("bp_test", "bp.sbc")))
            input.CopyTo(gz);
        var bp = BlueprintData.Load(file);
        Assert.Equal("Test Ship", bp.Name);
        Assert.Equal(8, bp.Grids.Single().Blocks.Count);
    }

    [Fact]
    public void Builder_reads_gzip_compressed_definition_files()
    {
        var ms = new MemoryStream();
        using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        using (var input = File.OpenRead(TestData.Fx("CubeBlocks_Test.sbc")))
            input.CopyTo(gz);
        var bytes = ms.ToArray();
        var b = new GameDatabaseBuilder();
        b.AddModFile("9", "Data/Blocks.sbc", () => new MemoryStream(bytes));
        Assert.Empty(b.Errors);
        Assert.NotEmpty(b.Build().ModBlocks["9"]);
    }

    [Fact]
    public void Cost_expands_non_component_intermediates_like_bottles()
    {
        var db = new GameDatabase();
        db.Recipes["Component/Tank"] = new() { ["OxygenContainerObject/HydrogenBottle"] = 2 };
        db.Recipes["OxygenContainerObject/HydrogenBottle"] = new() { ["Ingot/Iron"] = 80 };
        db.Recipes["Ingot/Iron"] = new() { ["Ore/Iron"] = 1 / 0.7 };
        var (ingots, ore) = new CostCalculator().Compute(new Dictionary<string, int> { ["Component/Tank"] = 1 }, db, Array.Empty<string>());
        Assert.Equal(160, ingots["Ingot/Iron"], 6);
        Assert.False(ingots.ContainsKey("OxygenContainerObject/HydrogenBottle"));
        Assert.False(ore.ContainsKey("Ingot/Iron"));
        Assert.Equal(160 / 0.7, ore["Ore/Iron"], 6);
    }

    [Fact]
    public void Ore_list_only_contains_ores()
    {
        var db = new GameDatabase();
        db.Recipes["Component/Plate"] = new() { ["Ingot/Exotic"] = 1 };
        db.Recipes["Ingot/Exotic"] = new() { ["Ore/Exotic"] = 2, ["OxygenContainerObject/HydrogenBottle"] = 0.1 };
        var (_, ore) = new CostCalculator().Compute(new Dictionary<string, int> { ["Component/Plate"] = 10 }, db, Array.Empty<string>());
        Assert.Equal(20, ore["Ore/Exotic"], 6);
        Assert.Single(ore);
    }

    [Fact]
    public void Bad_xml_throws_InvalidDataException()
    {
        var f = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sbc");
        File.WriteAllText(f, "<Definitions><broken");
        Assert.Throws<InvalidDataException>(() => BlueprintData.Load(f));
    }
}
