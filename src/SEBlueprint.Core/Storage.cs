namespace SEBlueprint.Core;

public static class Storage
{
    public const string PortableFolderName = "SEBlueprintInspector-data";

    public static string DefaultRoot { get; } = ResolveDefault();

    public static string Root { get; set; } = DefaultRoot;

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
