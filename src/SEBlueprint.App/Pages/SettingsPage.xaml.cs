using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SEBlueprint.App.Services;
using SEBlueprint.App.ViewModels;
using SEBlueprint.Core;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Updates;

namespace SEBlueprint.App.Pages;

public partial class SettingsPage : Page
{
    static AppState App => AppState.Current;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    void Refresh()
    {
        NotFoundBar.Visibility = !App.GameFound && !App.IsBusy ? Visibility.Visible : Visibility.Collapsed;
        CheckOnStart.IsChecked = App.Settings.CheckUpdatesOnStart;
        if (string.IsNullOrEmpty(UpdateStatus.Text)) UpdateStatus.Text = $"You have version {Updater.CurrentVersion.ToString(3)}.";
        BuildPaths();
    }

    void BuildPaths()
    {
        while (PathsPanel.Children.Count > 1) PathsPanel.Children.RemoveAt(1);
        var s = App.Settings;
        AddPathRow("Space Engineers folder", App.Paths.GameDir, s.GameDirOverride, v => s.GameDirOverride = v);
        AddPathRow("Workshop mods folder", App.Paths.WorkshopDir, s.WorkshopDirOverride, v => s.WorkshopDirOverride = v);
        AddPathRow("Space Engineers 2 blueprints", App.Paths.Se2BlueprintDir, s.Se2BlueprintDirOverride, v => s.Se2BlueprintDirOverride = v);
        foreach (var root in App.Paths.BlueprintRoots)
        {
            var custom = s.ExtraBlueprintRoots.Contains(root.Path, StringComparer.OrdinalIgnoreCase);
            AddInfoRow($"Blueprints ({root.Source})", root.Path, custom ? () => { s.ExtraBlueprintRoots.RemoveAll(p => string.Equals(p, root.Path, StringComparison.OrdinalIgnoreCase)); SaveAndReload(); } : null);
        }
    }

    void AddPathRow(string label, string? current, string? overrideValue, Action<string?> set)
    {
        var grid = Row(label, current ?? "not found", overrideValue != null ? "set manually" : "auto-detected");
        var browse = new Button { Content = "Browse…", Margin = new Thickness(8, 0, 0, 0) };
        browse.Click += (_, _) =>
        {
            var dlg = new OpenFolderDialog { Title = label };
            if (dlg.ShowDialog() != true) return;
            set(dlg.FolderName);
            SaveAndReload();
        };
        Grid.SetColumn(browse, 2);
        grid.Children.Add(browse);
        if (overrideValue != null)
        {
            var reset = new Button { Content = "Auto", Margin = new Thickness(8, 0, 0, 0), ToolTip = "Go back to automatic detection" };
            reset.Click += (_, _) => { set(null); SaveAndReload(); };
            Grid.SetColumn(reset, 3);
            grid.Children.Add(reset);
        }
        PathsPanel.Children.Add(grid);
    }

    void AddInfoRow(string label, string value, Action? remove)
    {
        var grid = Row(label, value, null);
        if (remove != null)
        {
            var b = new Button { Content = "Remove", Margin = new Thickness(8, 0, 0, 0) };
            b.Click += (_, _) => remove();
            Grid.SetColumn(b, 2);
            grid.Children.Add(b);
        }
        PathsPanel.Children.Add(grid);
    }

    static Grid Row(string label, string value, string? hint)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var l = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        l.Children.Add(new TextBlock { Text = label.ToUpperInvariant(), Style = (Style)Application.Current.Resources["Label"] });
        if (hint != null) l.Children.Add(new TextBlock { Text = hint, FontSize = 11, Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextFaint"] });
        grid.Children.Add(l);
        var v = new TextBlock { Text = value, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = value };
        Grid.SetColumn(v, 1);
        grid.Children.Add(v);
        return grid;
    }

    void OnAddFolder(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Folder that contains blueprint folders" };
        if (dlg.ShowDialog() != true) return;
        if (!App.Settings.ExtraBlueprintRoots.Contains(dlg.FolderName, StringComparer.OrdinalIgnoreCase))
            App.Settings.ExtraBlueprintRoots.Add(dlg.FolderName);
        SaveAndReload();
    }

    async void OnRebuild(object sender, RoutedEventArgs e)
    {
        await App.InitializeAsync(rebuildCache: true);
        Refresh();
    }

    async void SaveAndReload()
    {
        App.Settings.Save();
        await App.InitializeAsync();
        Refresh();
    }

    void OnReportBug(object sender, RoutedEventArgs e) => Links.Open(Links.ReportBug);
    void OnStar(object sender, RoutedEventArgs e) => Links.Open(Links.Repo);
    void OnReleases(object sender, RoutedEventArgs e) => Links.Open(Links.Releases);

    void OnCheckOnStart(object sender, RoutedEventArgs e)
    {
        App.Settings.CheckUpdatesOnStart = CheckOnStart.IsChecked == true;
        App.Settings.Save();
    }

    async void OnCheckNow(object sender, RoutedEventArgs e)
    {
        CheckNowButton.IsEnabled = false;
        UpdateStatus.Text = "Checking GitHub…";
        try
        {
            var release = await Updater.CheckAsync();
            var current = Updater.CurrentVersion.ToString(3);
            if (release == null) UpdateStatus.Text = "Couldn't read the latest release. Try the releases page.";
            else if (!ReleaseFeed.IsNewer(release, Updater.CurrentVersion)) UpdateStatus.Text = $"You're up to date (version {current}).";
            else
            {
                UpdateStatus.Text = $"Version {release.Version.ToString(3)} is available (you have {current}).";
                MainWindow.Instance?.ShowUpdate(release);
            }
        }
        catch (Exception ex)
        {
            Log.Write($"Update check failed: {ex.Message}");
            UpdateStatus.Text = "Couldn't reach GitHub. Check your connection or open the releases page.";
        }
        finally { CheckNowButton.IsEnabled = true; }
    }

    static void Try(Action a)
    {
        try { a(); }
        catch (Exception ex)
        {
            Log.Write($"Settings action failed: {ex.Message}");
            App.ErrorMessage = ex.Message;
        }
    }
}
