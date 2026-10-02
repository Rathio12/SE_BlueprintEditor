using System.Security.Cryptography;
using System.Text;

namespace SEBlueprint.Core.Data;

/// <summary>Loads the game database from a local Space Engineers install and Workshop folder, with a JSON cache.</summary>
public static class GameDatabaseLoader
{
    const string FormatVersion = "1";

    public static GameDatabase Load(string? dataDir, string? workshopDir, IProgress<string>? progress = null)
    {
        var b = new GameDatabaseBuilder();
        if (Exists(dataDir))
        {
            progress?.Report("Reading vanilla game data…");
            foreach (var f in Files(dataDir!, "*.sbc", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(dataDir!, f);
                var top = rel.Split(Path.DirectorySeparatorChar)[0];
                var wanted = top == "CubeBlocks" || top == "Blueprints" || rel.StartsWith("Blueprints", StringComparison.OrdinalIgnoreCase)
                             || (!rel.Contains(Path.DirectorySeparatorChar) && (top.StartsWith("Components") || top.StartsWith("PhysicalItems") || top.StartsWith("AmmoMagazines")));
                if (wanted) b.AddVanillaFile(rel, () => File.OpenRead(f));
            }
            var texts = Path.Combine(dataDir!, "Localization", "MyTexts.resx");
            if (File.Exists(texts)) b.AddVanillaFile("Localization/MyTexts.resx", () => File.OpenRead(texts));
        }

        // Mods are independent, so they are parsed in parallel and merged afterwards in a fixed order.
        var mods = ModFolders(workshopDir);
        var perMod = new GameDatabaseBuilder[mods.Count];
        var done = 0;
        Parallel.For(0, mods.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount) }, i =>
        {
            perMod[i] = LoadMod(mods[i]);
            progress?.Report($"Reading mods… {Interlocked.Increment(ref done)}/{mods.Count}");
        });
        foreach (var m in perMod) b.Merge(m);

        var db = b.Build();
        db.CacheKey = ComputeCacheKey(dataDir, workshopDir);
        return db;
    }

    static GameDatabaseBuilder LoadMod(string mod)
    {
        var b = new GameDatabaseBuilder();
        var id = Path.GetFileName(mod);
        b.AddModName(id, id);
        var data = Path.Combine(mod, "Data");
        foreach (var f in Files(data, "*.sbc", SearchOption.AllDirectories))
            b.AddModFile(id, Path.GetRelativePath(mod, f), () => File.OpenRead(f));
        foreach (var f in Files(Path.Combine(data, "Scripts"), "*.cs", SearchOption.AllDirectories))
            b.AddModFile(id, Path.GetRelativePath(mod, f), () => File.OpenRead(f));
        var texts = Path.Combine(data, "Localization", "MyTexts.resx");
        if (File.Exists(texts)) b.AddModFile(id, "Data/Localization/MyTexts.resx", () => File.OpenRead(texts));
        return b;
    }

    public static GameDatabase LoadCached(string? dataDir, string? workshopDir, string cacheFile, IProgress<string>? progress = null)
    {
        var key = ComputeCacheKey(dataDir, workshopDir);
        try
        {
            if (File.Exists(cacheFile))
            {
                var cached = GameDatabase.FromJson(File.ReadAllText(cacheFile));
                if (cached.CacheKey == key) return cached;
            }
        }
        catch (Exception ex) { Log.Write($"Ignoring unreadable cache {cacheFile}: {ex.Message}"); }

        var db = Load(dataDir, workshopDir, progress);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cacheFile))!);
            File.WriteAllText(cacheFile, db.ToJson());
        }
        catch (Exception ex) { Log.Write($"Could not write cache {cacheFile}: {ex.Message}"); }
        return db;
    }

    /// <summary>Changes whenever the game data, the set of mods, a mod's folder or this library's version changes.</summary>
    public static string ComputeCacheKey(string? dataDir, string? workshopDir)
    {
        var sb = new StringBuilder();
        sb.Append(FormatVersion).Append('|').Append(typeof(GameDatabaseLoader).Assembly.GetName().Version).Append('|').Append(dataDir).Append('|');
        if (Exists(dataDir))
        {
            var newest = Files(dataDir!, "*.sbc", SearchOption.AllDirectories).Select(SafeWriteTime).DefaultIfEmpty().Max();
            sb.Append(newest.Ticks);
        }
        foreach (var mod in ModFolders(workshopDir))
            sb.Append('|').Append(Path.GetFileName(mod)).Append(':')
              .Append(SafeWriteTime(mod).Ticks).Append(':').Append(SafeWriteTime(Path.Combine(mod, "Data")).Ticks);
        return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    /// <summary>Workshop item folders that contain a Data folder (i.e. mods, not blueprints or worlds).</summary>
    public static List<string> ModFolders(string? workshopDir)
    {
        if (!Exists(workshopDir)) return new();
        try
        {
            return Directory.EnumerateDirectories(workshopDir!)
                .Where(d => Directory.Exists(Path.Combine(d, "Data")))
                .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) { Log.Write($"Cannot list mods in {workshopDir}: {ex.Message}"); return new(); }
    }

    static bool Exists(string? dir)
    {
        try { return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir); }
        catch (Exception) { return false; }
    }

    static IEnumerable<string> Files(string dir, string pattern, SearchOption option)
    {
        if (!Exists(dir)) return Array.Empty<string>();
        try
        {
            return Directory.EnumerateFiles(dir, pattern, new EnumerationOptions
            {
                RecurseSubdirectories = option == SearchOption.AllDirectories,
                IgnoreInaccessible = true,
            }).ToList();
        }
        catch (Exception ex) { Log.Write($"Cannot list {dir}: {ex.Message}"); return Array.Empty<string>(); }
    }

    static DateTime SafeWriteTime(string path)
    {
        try { return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : Directory.GetLastWriteTimeUtc(path); }
        catch (Exception) { return DateTime.MinValue; }
    }
}
