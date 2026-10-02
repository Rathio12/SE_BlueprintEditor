using SEBlueprint.Core.Data;

namespace SEBlueprint.Core.Tests;

public class GameDatabaseTests
{
    [Fact]
    public void Loads_vanilla_recipes_from_blueprints_folder_and_mods()
    {
        var (data, ws) = TestData.Fake();
        var db = GameDatabaseLoader.Load(data, ws);
        Assert.True(db.Blocks.ContainsKey("CargoContainer/LargeBlockLargeContainer"));
        Assert.Null(db.Blocks["CargoContainer/LargeBlockLargeContainer"].ModId);
        Assert.True(db.Recipes.ContainsKey("Component/SteelPlate"));
        Assert.Equal(BlockCategory.FixedWeapon, db.ModBlocks[TestData.ModId]["ConveyorSorter/M12Swarm"].Category);
        Assert.Contains(TestData.ModId, db.ModNames.Keys);
    }

    [Fact]
    public void Localizes_display_names()
    {
        var (data, ws) = TestData.Fake();
        var db = GameDatabaseLoader.Load(data, ws);
        Assert.Equal("Steel Plate", db.Items["Component/SteelPlate"].DisplayName);
        Assert.Equal("Gatling Turret", db.Blocks["LargeGatlingTurret/"].DisplayName);
    }

    [Fact]
    public void Resolve_prefers_enabled_mod_then_vanilla_then_any_mod()
    {
        var (data, ws) = TestData.Fake();
        var db = GameDatabaseLoader.Load(data, ws);
        Assert.Equal(TestData.ModId, db.Resolve("ConveyorSorter/M12Swarm", new[] { TestData.ModId }, out var u1)!.ModId);
        Assert.False(u1);
        Assert.Null(db.Resolve("LargeGatlingTurret/", Array.Empty<string>(), out _)!.ModId);
        Assert.Null(db.Resolve("Nope/Nope", Array.Empty<string>(), out _));
    }

    [Fact]
    public void Resolve_falls_back_to_unlisted_mod()
    {
        var (data, ws) = TestData.Fake();
        var db = GameDatabaseLoader.Load(data, ws);
        db.Blocks.Remove("ConveyorSorter/M12Swarm");
        var def = db.Resolve("ConveyorSorter/M12Swarm", Array.Empty<string>(), out var fromUnlisted);
        Assert.NotNull(def);
        Assert.True(fromUnlisted);
    }

    [Fact]
    public void Mod_recipes_and_items_do_not_leak_into_vanilla()
    {
        var b = new GameDatabaseBuilder();
        b.AddVanillaFile("Blueprints.sbc", () => File.OpenRead(TestData.Fx("Blueprints_Test.sbc")));
        b.AddVanillaFile("Components.sbc", () => File.OpenRead(TestData.Fx("Components_Test.sbc")));
        const string cheap = "<Definitions><Blueprints><Blueprint><Id><TypeId>BlueprintDefinition</TypeId><SubtypeId>SteelPlate</SubtypeId></Id><Prerequisites><Item Amount=\"7\" TypeId=\"Ingot\" SubtypeId=\"Iron\" /></Prerequisites><Result Amount=\"1\" TypeId=\"Component\" SubtypeId=\"SteelPlate\" /></Blueprint></Blueprints><Components><Component><Id><TypeId>Component</TypeId><SubtypeId>SteelPlate</SubtypeId></Id><Mass>99</Mass></Component></Components></Definitions>";
        b.AddModFile("cheap", "Data/Recipes.sbc", () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(cheap)));
        var db = b.Build();
        Assert.Equal(21, db.Recipes["Component/SteelPlate"]["Ingot/Iron"]);
        Assert.Equal(21, db.ResolveRecipe("Component/SteelPlate", Array.Empty<string>())!["Ingot/Iron"]);
        Assert.Equal(7, db.ResolveRecipe("Component/SteelPlate", new[] { "cheap" })!["Ingot/Iron"]);
        Assert.Equal(20, db.ResolveItem("Component/SteelPlate", Array.Empty<string>())!.Mass);
        Assert.Equal(99, db.ResolveItem("Component/SteelPlate", new[] { "cheap" })!.Mass);
    }

    [Fact]
    public void Cache_roundtrip_and_invalidation()
    {
        var (data, ws) = TestData.Fake();
        var cache = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var a = GameDatabaseLoader.LoadCached(data, ws, cache);
        Assert.True(File.Exists(cache));
        var b = GameDatabaseLoader.LoadCached(data, ws, cache);
        Assert.Equal(a.Blocks.Count, b.Blocks.Count);
        Assert.Equal(a.CacheKey, b.CacheKey);
        Assert.Equal(BlockCategory.FixedWeapon, b.ModBlocks[TestData.ModId]["ConveyorSorter/M12Swarm"].Category);

        Directory.CreateDirectory(Path.Combine(ws, "777", "Data"));
        Assert.NotEqual(a.CacheKey, GameDatabaseLoader.ComputeCacheKey(data, ws));
    }

    [Fact]
    public void Corrupt_cache_file_is_ignored()
    {
        var (data, ws) = TestData.Fake();
        var cache = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(cache, "{ definitely not json");
        var db = GameDatabaseLoader.LoadCached(data, ws, cache);
        Assert.NotEmpty(db.Blocks);
    }

    [Fact]
    public void Builder_from_streams_matches_disk_load_and_json_roundtrips()
    {
        var b = new GameDatabaseBuilder();
        b.AddVanillaFile("CubeBlocks/CubeBlocks_Test.sbc", () => File.OpenRead(TestData.Fx("CubeBlocks_Test.sbc")));
        b.AddVanillaFile("Components.sbc", () => File.OpenRead(TestData.Fx("Components_Test.sbc")));
        b.AddVanillaFile("Blueprints/Blueprints_Test.sbc", () => File.OpenRead(TestData.Fx("Blueprints_Test.sbc")));
        b.AddVanillaFile("Localization/MyTexts.resx", () => File.OpenRead(TestData.Fx("MyTexts.resx")));
        b.AddModFile("42", "Data/Blocks.sbc", () => File.OpenRead(TestData.Fx("CubeBlocks_Test.sbc")));
        b.AddModFile("42", "Data/Scripts/Part.cs", () => File.OpenRead(TestData.Fx("WeaponCorePart.cs.txt")));
        b.AddModFile("42", "Data/Broken.sbc", () => new MemoryStream("<x"u8.ToArray()));
        var db = b.Build();
        Assert.Single(b.Errors);

        var (data, ws) = TestData.Fake();
        var disk = GameDatabaseLoader.Load(data, ws);
        Assert.Equal(disk.Blocks.Keys.OrderBy(x => x), db.Blocks.Keys.OrderBy(x => x));
        Assert.Equal(disk.Recipes.Keys.OrderBy(x => x), db.Recipes.Keys.OrderBy(x => x));
        Assert.Equal(BlockCategory.FixedWeapon, db.ModBlocks["42"]["ConveyorSorter/M12Swarm"].Category);

        var copy = GameDatabase.FromJson(db.ToJson());
        Assert.Equal(db.Blocks.Count, copy.Blocks.Count);
        Assert.Equal("Steel Plate", copy.Items["Component/SteelPlate"].DisplayName);
    }

    [Fact]
    public void Builder_can_extend_an_existing_database_with_mods()
    {
        var b = new GameDatabaseBuilder();
        b.AddVanillaFile("CubeBlocks/CubeBlocks_Test.sbc", () => File.OpenRead(TestData.Fx("CubeBlocks_Test.sbc")));
        var vanilla = b.Build();
        var ext = new GameDatabaseBuilder(vanilla);
        ext.AddModFile("42", "Data/Blocks.sbc", () => File.OpenRead(TestData.Fx("CubeBlocks_Test.sbc")));
        var db = ext.Build();
        Assert.Equal(vanilla.Blocks.Count, db.Blocks.Count);
        Assert.True(db.ModBlocks.ContainsKey("42"));
    }

    [Fact]
    public void Missing_directories_give_empty_database()
    {
        var db = GameDatabaseLoader.Load(null, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        Assert.Empty(db.Blocks);
        Assert.Empty(db.ModBlocks);
    }
}
