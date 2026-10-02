using System.IO;
using System.Windows;
using System.Windows.Controls;
using SEBlueprint.App.Pages;
using SEBlueprint.App.Services;
using SEBlueprint.Core;

namespace SEBlueprint.App;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    readonly BlueprintsPage _blueprints = new();

    public MainWindow()
    {
        Instance = this;
        InitializeComponent();
        DataContext = AppState.Current;
        VersionText.Text = $"v{typeof(MainWindow).Assembly.GetName().Version?.ToString(3)}";
        StateChanged += (_, _) =>
        {
            MaxButton.Content = WindowState == WindowState.Maximized ? "" : "";

            Root.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
        };
        Loaded += OnLoaded;
    }

    async void OnLoaded(object sender, RoutedEventArgs e)
    {
        TabBlueprints.IsChecked = true;
        await AppState.Current.InitializeAsync();
        if (!AppState.Current.GameFound) TabSettings.IsChecked = true;
        HandleArguments(Environment.GetCommandLineArgs().Skip(1).ToList());
    }

    void OnTab(object sender, RoutedEventArgs e)
    {
        Page page = sender switch
        {
            _ when sender == TabProfiles => new ProfilesPage(),
            _ when sender == TabSettings => new SettingsPage(),
            _ when sender == TabAbout => new AboutPage(),
            _ => _blueprints,
        };
        PageHost.Navigate(page);
    }

    void HandleArguments(List<string> args)
    {
        try
        {
            var page = args.IndexOf("--page") is var i and >= 0 && i + 1 < args.Count ? args[i + 1].ToLowerInvariant() : null;
            var target = args.FirstOrDefault(a => !a.StartsWith("--") && a != page);
            if (target != null)
            {
                var folder = File.Exists(target) ? Path.GetDirectoryName(Path.GetFullPath(target)) : target;
                var row = AppState.Current.Rows.FirstOrDefault(r =>
                              string.Equals(SafeFull(r.Entry.Folder), SafeFull(folder), StringComparison.OrdinalIgnoreCase))
                          ?? AppState.Current.Rows.FirstOrDefault(r => string.Equals(r.Name, target, StringComparison.OrdinalIgnoreCase));
                if (row != null)
                {
                    TabBlueprints.IsChecked = true;
                    _blueprints.Select(row);
                }
            }
            switch (page)
            {
                case "profiles": TabProfiles.IsChecked = true; break;
                case "settings": TabSettings.IsChecked = true; break;
                case "info": TabAbout.IsChecked = true; break;
            }
        }
        catch (Exception ex) { Log.Write($"Bad command line: {ex.Message}"); }
    }

    static string? SafeFull(string? p)
    {
        try { return p == null ? null : Path.GetFullPath(p); }
        catch (Exception) { return p; }
    }

    void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    void OnMaximize(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    void OnClose(object sender, RoutedEventArgs e) => Close();
    void OnDismissError(object sender, RoutedEventArgs e) => AppState.Current.ErrorMessage = null;
}
