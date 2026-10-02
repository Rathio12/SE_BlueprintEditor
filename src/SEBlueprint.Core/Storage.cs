namespace SEBlueprint.Core;

/// <summary>
/// Where the app keeps its own files (settings, limit profiles, game-data cache, log).
/// The desktop app is portable: it uses a folder next to the .exe, falling back to %LocalAppData% when that is not writable.
/// </summary>
public static class Storage
{
    public const string PortableFolderName = "SEBlueprintInspector-data";

    public static string DefaultRoot { get; } = ResolveDefault();

    public static string Root { get; set; } = DefaultRoot;

    /// <summary>Returns "&lt;exeDir&gt;/SEBlueprintInspector-data" when it can be created and written, otherwise <see cref="DefaultRoot"/>.</summary>
    public static string ChoosePortableRoot(string exeDir)
    {
        try
        {
            var root = Path.Combine(exeDir, PortableFolderName);
            Directory.CreateDirectory(root);
            var probe = Path.Combine(root, ".write-test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return root;
        }
        catch (Exception) { return DefaultRoot; }
    }

    static string ResolveDefault()
    {
        try
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local)) local = Path.GetTempPath();
            return Path.Combine(local, "SEBlueprintInspector");
        }
        catch (Exception) { return Path.Combine(Path.GetTempPath(), "SEBlueprintInspector"); }
    }
}
