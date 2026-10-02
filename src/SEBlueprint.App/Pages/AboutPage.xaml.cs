using System.Windows;
using System.Windows.Controls;
using SEBlueprint.App.Services;
using SEBlueprint.Core;

namespace SEBlueprint.App.Pages;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        VersionText.Text = $"Version {typeof(AboutPage).Assembly.GetName().Version?.ToString(3)}";
    }

    void OnGitHub(object sender, RoutedEventArgs e) => Links.Open(Links.Repo);

    void OnDataFolder(object sender, RoutedEventArgs e) => Links.Open(Log.Dir);
}
