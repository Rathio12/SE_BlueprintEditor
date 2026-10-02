using System.Text.Json;
using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Library;
using SEBlueprint.Core.Paths;

namespace SEBlueprint.Core.Games;

public sealed class Se2Adapter(GamePaths paths) : IGameAdapter
{
    const string MetadataFile = ".container-info";

    public GameId Game => GameId.SE2;
    public bool IsInstalled => paths.Se2GameDir != null || paths.Se2BlueprintDir != null;
    public bool CanAnalyze => false;

    public IReadOnlyList<BlueprintEntry> Enumerate()
    {
        var dir = paths.Se2BlueprintDir;
        try
        {
            if (dir == null || !Directory.Exists(dir)) return Array.Empty<BlueprintEntry>();
            return Directory.EnumerateDirectories(dir)
                .Where(d => File.Exists(Path.Combine(d, MetadataFile)))
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .Select(ReadEntry)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot list SE2 blueprints in {dir}: {ex.Message}");
            return Array.Empty<BlueprintEntry>();
        }
    }

    public static BlueprintEntry ReadEntry(string folder)
    {
        var fallback = Path.GetFileName(folder);
        var file = Path.Combine(folder, MetadataFile);
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var meta = doc.RootElement.GetProperty("$Value").GetProperty("Meta");
            var baseMeta = Prop(meta, "BaseMetadata");
            var title = Str(baseMeta, "Title") ?? Str(Prop(Prop(meta, "UserInfo"), "DisplayName"), "RawText");
            var name = string.IsNullOrWhiteSpace(title) ? fallback : title!;
            var edited = DateTime.TryParse(Str(meta, "LastEdited"), out var t) ? t : SafeWriteTime(file);
            var blocks = Int(meta, "BlockCount");
            var pcu = Int(meta, "PCU");
            return new BlueprintEntry
            {
                Game = GameId.SE2,
                Folder = folder,
                Source = "SE2",
                Name = name,
                Description = Str(baseMeta, "Description"),
                Modified = edited,
                Report = blocks == null && pcu == null ? null : new BlueprintReport
                {
                    Name = name,
                    IsPartial = true,
                    Blocks = blocks ?? 0,
                    Pcu = pcu ?? 0,
                },
            };
        }
        catch (Exception ex)
        {
            Log.Write($"Cannot read SE2 blueprint {file}: {ex.Message}");
            return new BlueprintEntry
            {
                Game = GameId.SE2,
                Folder = folder,
                Source = "SE2",
                Name = fallback,
                Modified = SafeWriteTime(file),
                Error = $"Unreadable SE2 blueprint metadata: {ex.Message}",
            };
        }
    }

    static JsonElement Prop(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v : default;

    static string? Str(JsonElement e, string name)
    {
        var v = Prop(e, name);
        return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    static int? Int(JsonElement e, string name)
    {
        var v = Prop(e, name);
        return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : null;
    }

    static DateTime SafeWriteTime(string path)
    {
        try { return File.GetLastWriteTime(path); }
        catch (Exception) { return DateTime.MinValue; }
    }
}
