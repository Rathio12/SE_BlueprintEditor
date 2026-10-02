using System.Text.Json;
using BCnEncoder.Decoder;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Imaging;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Paths;

var outDir = args.Length > 0 ? args[0] : Path.Combine("src", "SEBlueprint.Web", "wwwroot", "data");
var iconDir = args.Length > 1 ? args[1] : Path.Combine("src", "SEBlueprint.Web", "wwwroot", "icons");
var paths = SteamLocator.Detect();
if (paths.DataDir == null || paths.GameDir == null)
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

Directory.CreateDirectory(iconDir);
var decoder = new BcDecoder();
var exported = 0;
foreach (var icon in db.Items.Values.Select(i => i.Icon).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct())
{
    var source = Path.Combine(paths.GameDir, "Content", icon!.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(source)) continue;
    try
    {
        using var fs = File.OpenRead(source);
        var image = decoder.Decode2D(fs);
        var rgba = new byte[image.Width * image.Height * 4];
        var span = image.Span;
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
            {
                var c = span[y, x];
                var i = (y * image.Width + x) * 4;
                rgba[i] = c.r;
                rgba[i + 1] = c.g;
                rgba[i + 2] = c.b;
                rgba[i + 3] = c.a;
            }
        File.WriteAllBytes(Path.Combine(iconDir, IconFiles.FileName(icon)), Png.Encode(image.Width, image.Height, rgba));
        exported++;
    }
    catch (Exception ex) { Console.Error.WriteLine($"Skipped icon {icon}: {ex.Message}"); }
}

Console.WriteLine($"Exported {db.Blocks.Count} blocks, {db.Items.Count} items, {db.Recipes.Count} recipes, {profiles.Count} profiles, {exported} item icons");
return 0;
