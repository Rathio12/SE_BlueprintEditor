namespace SEBlueprint.Core;

/// <summary>Local, append-only diagnostic log. Never throws and never sends anything anywhere.</summary>
public static class Log
{
    static readonly object Gate = new();

    public static string Dir { get; } = ResolveDir();

    public static string FilePath => Path.Combine(Dir, "log.txt");

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Dir);
                var file = FilePath;
                if (File.Exists(file) && new FileInfo(file).Length > 2_000_000)
                    File.Delete(file);
                File.AppendAllText(file, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }
        catch (Exception) { /* logging must never crash the app */ }
    }

    static string ResolveDir()
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
