using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using SEBlueprint.Core;

namespace SEBlueprint.App.Pages;

public partial class AboutPage : Page
{
    public const string GitHubUrl = "https://github.com/Rathio12/SE_BlueprintEditor";

    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"Version {typeof(AboutPage).Assembly.GetName().Version?.ToString(3)}";
    }

    void OnGitHub(object sender, RoutedEventArgs e) => Open(GitHubUrl);

    void OnDataFolder(object sender, RoutedEventArgs e) => Open(Log.Dir);

    static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch (Exception ex) { Log.Write($"Cannot open {target}: {ex.Message}"); }
    }
}
