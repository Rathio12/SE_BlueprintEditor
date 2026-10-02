using System.Text.Json;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Paths;

var outDir = args.Length > 0 ? args[0] : Path.Combine("src", "SEBlueprint.Web", "wwwroot", "data");
var paths = SteamLocator.Detect();
if (paths.DataDir == null)
{
    Console.Error.WriteLine("Space Engineers was not found.");
    return 1;
}

Directory.CreateDirectory(outDir);
var db = GameDatabaseLoader.Load(paths.DataDir, null);
db.CacheKey = "";
File.WriteAllText(Path.Combine(outDir, "vanilla-db.json"), db.ToJson());

var profiles = VanillaProfiles.Load(paths.CustomWorldsDir);
File.WriteAllText(Path.Combine(outDir, "vanilla-profiles.json"), JsonSerializer.Serialize(profiles));

Console.WriteLine($"Exported {db.Blocks.Count} blocks, {db.Items.Count} items, {db.Recipes.Count} recipes, {profiles.Count} profiles to {outDir}");
return 0;
