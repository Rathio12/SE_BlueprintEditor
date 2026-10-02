using System.Diagnostics;
using SEBlueprint.Core;

namespace SEBlueprint.App.Services;

public static class Links
{
    public const string Repo = "https://github.com/Rathio12/SE_BlueprintEditor";
    public const string ReportBug = Repo + "/issues/new/choose";
    public const string Releases = Repo + "/releases";

    public static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception ex) { Log.Write($"Cannot open {target}: {ex.Message}"); }
    }
}
