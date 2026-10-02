using System.Text.Json;

namespace SEBlueprint.Core.Limits;

/// <summary>Saves your own limit profiles as JSON files (one per profile) so they can be shared with server players.</summary>
public static class ProfileStore
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string Dir { get; } = ResolveDir();

    public static string ToJson(LimitProfile p) => JsonSerializer.Serialize(p, Json);

    public static LimitProfile FromJson(string json)
    {
        var p = JsonSerializer.Deserialize<LimitProfile>(json) ?? throw new JsonException("Empty profile");
        p.BuiltIn = false;
        p.BlockTypeLimits ??= new();
        if (string.IsNullOrWhiteSpace(p.Name)) p.Name = "Imported profile";
        return p;
    }

    public static IReadOnlyList<LimitProfile> LoadUser(string? dir = null)
    {
        dir ??= Dir;
        var list = new List<LimitProfile>();
        try
        {
            if (!Directory.Exists(dir)) return list;
            foreach (var f in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                try { list.Add(FromJson(File.ReadAllText(f))); }
                catch (Exception ex) { Log.Write($"Skipped profile {f}: {ex.Message}"); }
            }
        }
        catch (Exception ex) { Log.Write($"Cannot read profiles in {dir}: {ex.Message}"); }
        return list;
    }

    public static void Save(LimitProfile p, string? dir = null)
    {
        dir ??= Dir;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, FileName(p.Name)), ToJson(p));
    }

    public static void Delete(string name, string? dir = null)
    {
        var file = Path.Combine(dir ?? Dir, FileName(name));
        if (File.Exists(file)) File.Delete(file);
    }

    static string FileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }).ToHashSet();
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return (safe.Length == 0 ? "profile" : safe) + ".json";
    }

    static string ResolveDir()
    {
        try
        {
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(roaming)) roaming = Path.GetTempPath();
            return Path.Combine(roaming, "SEBlueprintInspector", "profiles");
        }
        catch (Exception) { return Path.Combine(Path.GetTempPath(), "SEBlueprintInspector", "profiles"); }
    }
}
