using System.Text.RegularExpressions;

namespace SEBlueprint.Core.Paths;

/// <summary>Finds Steam libraries, the Space Engineers install, Workshop content and blueprint folders. Never throws.</summary>
public static class SteamLocator
{
    public const string Se1AppId = "244850";
    static readonly Regex PathEntry = new(@"""path""\s+""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);

    public static IReadOnlyList<string> ParseLibraryFolders(string vdfText) =>
        PathEntry.Matches(vdfText).Select(m => m.Groups[1].Value.Replace("\\\\", "\\")).ToList();

    public static GamePaths Detect(string? steamRootOverride = null, string? appDataOverride = null)
    {
        var appData = appDataOverride ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var libraries = new List<string>();
        try
        {
            var steam = steamRootOverride ?? FindSteamRoot();
            if (steam != null)
            {
                libraries.Add(steam);
                var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdf))
                    libraries.AddRange(ParseLibraryFolders(File.ReadAllText(vdf)));
            }
        }
        catch (Exception) { /* fall through with whatever was found */ }
        return Build(libraries.Distinct(StringComparer.OrdinalIgnoreCase), appData);
    }

    public static GamePaths Build(IEnumerable<string> libraries, string appData)
    {
        string? game = null, workshop = null;
        foreach (var lib in libraries)
        {
            try
            {
                var g = Path.Combine(lib, "steamapps", "common", "SpaceEngineers");
                if (game == null && Directory.Exists(g)) game = g;
                var w = Path.Combine(lib, "steamapps", "workshop", "content", Se1AppId);
                if (workshop == null && Directory.Exists(w)) workshop = w;
            }
            catch (Exception) { }
        }

        var roots = new List<BlueprintRoot>();
        void Add(string? path, string source)
        {
            try { if (path != null && Directory.Exists(path)) roots.Add(new BlueprintRoot(path, source)); }
            catch (Exception) { }
        }
        var bp = Path.Combine(appData, "SpaceEngineers", "Blueprints");
        Add(Path.Combine(bp, "local"), "Local");
        Add(Path.Combine(bp, "cloud"), "Cloud");
        Add(Path.Combine(bp, "workshop"), "Workshop");
        Add(workshop, "Workshop");
        return new GamePaths(game, workshop, roots);
    }

    static string? FindSteamRoot()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) is string p
                    && Directory.Exists(p))
                    return p.Replace('/', Path.DirectorySeparatorChar);
            }
            catch (Exception) { }
        }
        const string fallback = @"C:\Program Files (x86)\Steam";
        return Directory.Exists(fallback) ? fallback : null;
    }
}
