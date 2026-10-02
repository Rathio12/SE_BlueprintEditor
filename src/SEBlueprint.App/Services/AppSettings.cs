using System.IO;
using System.Text.Json;
using SEBlueprint.Core;

namespace SEBlueprint.App.Services;

public sealed class AppSettings
{
    public string? GameDirOverride { get; set; }
    public string? WorkshopDirOverride { get; set; }
    public string? Se2BlueprintDirOverride { get; set; }
    public List<string> ExtraBlueprintRoots { get; set; } = new();
    public string? ActiveProfileName { get; set; }
    public double AssemblerEfficiency { get; set; } = 1;
    public int YieldModules { get; set; }
    public bool RatePromptDone { get; set; }
    public bool CheckUpdatesOnStart { get; set; }
    public string? SkippedUpdateVersion { get; set; }

    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(Storage.Root, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch (Exception ex) { Log.Write($"Settings unreadable, using defaults: {ex.Message}"); }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception ex) { Log.Write($"Cannot save settings: {ex.Message}"); }
    }

    public double YieldMultiplier => YieldModules switch { 1 => 1.19, 2 => 1.41, 3 => 1.68, >= 4 => 2.0, _ => 1.0 };
}
