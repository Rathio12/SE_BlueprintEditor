using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SEBlueprint.App.Pages;
using SEBlueprint.App.Services;
using SEBlueprint.Core;
using SEBlueprint.Core.Updates;

namespace SEBlueprint.App;

public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    readonly BlueprintsPage _blueprints = new();
    ReleaseInfo? _update;

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
        _ = CheckForUpdateOnStart();
        await AppState.Current.InitializeAsync();
        if (!AppState.Current.GameFound) TabSettings.IsChecked = true;
        HandleArguments(Environment.GetCommandLineArgs().Skip(1).ToList());
        await MaybeAskForRating();
    }

    async Task MaybeAskForRating()
    {
        if (!RatePrompt.ShouldShow(AppState.Current.Settings)) return;
        await Task.Delay(RatePrompt.RandomDelay());
        if (UpdatePanel.Visibility == Visibility.Visible) return;
        RatePanel.Visibility = Visibility.Visible;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        RatePanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
        RateSlide.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });
    }

    void HideRatePanel()
    {
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        fade.Completed += (_, _) => RatePanel.Visibility = Visibility.Collapsed;
        RatePanel.BeginAnimation(OpacityProperty, fade);
    }

    void OnRateStar(object sender, RoutedEventArgs e)
    {
        Links.Open(Links.Repo);
        RatePrompt.Finish(AppState.Current.Settings);
        HideRatePanel();
    }

    void OnRateLater(object sender, RoutedEventArgs e) => HideRatePanel();

    void OnRateNever(object sender, RoutedEventArgs e)
    {
        RatePrompt.Finish(AppState.Current.Settings);
        HideRatePanel();
    }

    async Task CheckForUpdateOnStart()
    {
        var settings = AppState.Current.Settings;
        if (!settings.CheckUpdatesOnStart) return;
        try
        {
            var release = await Updater.CheckAsync();
            if (release != null && ReleaseFeed.IsNewer(release, Updater.CurrentVersion) && release.Tag != settings.SkippedUpdateVersion)
                ShowUpdate(release);
        }
        catch (Exception ex) { Log.Write($"Update check failed: {ex.Message}"); }
    }

    public void ShowUpdate(ReleaseInfo release)
    {
        _update = release;
        UpdateTitle.Text = $"VERSION {release.Version.ToString(3)} IS AVAILABLE";
        UpdateNotes.Text = ReleaseNotes(release.Notes);
        var canInstall = release.CanInstall && Updater.CanReplaceExe;
        UpdateInstall.Visibility = canInstall ? Visibility.Visible : Visibility.Collapsed;
        UpdateHint.Text = canInstall
            ? $"You have {Updater.CurrentVersion.ToString(3)}. The new exe is downloaded from GitHub, checked against the release checksum and replaces this one."
            : $"You have {Updater.CurrentVersion.ToString(3)}. Download the new exe from the release page.";
        UpdateProgress.Visibility = Visibility.Collapsed;
        UpdateButtons.IsEnabled = UpdateClose.IsEnabled = true;
        RatePanel.Visibility = Visibility.Collapsed;
        UpdatePanel.Visibility = Visibility.Visible;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        UpdatePanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
        UpdateSlide.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(320)) { EasingFunction = ease });
    }

    static string ReleaseNotes(string markdown)
    {
        var lines = markdown.Replace("\r", "").Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(l => l.StartsWith("### ") ? l[4..].ToUpperInvariant() : l.StartsWith("- ") ? "•  " + l[2..] : l)
            .Take(10);
        return string.Join(Environment.NewLine, lines);
    }

    void HideUpdatePanel()
    {
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        fade.Completed += (_, _) => UpdatePanel.Visibility = Visibility.Collapsed;
        UpdatePanel.BeginAnimation(OpacityProperty, fade);
    }

    async void OnUpdateInstall(object sender, RoutedEventArgs e)
    {
        if (_update == null) return;
        UpdateButtons.IsEnabled = UpdateClose.IsEnabled = false;
        UpdateProgress.Value = 0;
        UpdateProgress.Visibility = Visibility.Visible;
        UpdateHint.Text = "Downloading…";
        try
        {
            await Updater.InstallAsync(_update, new Progress<double>(v => UpdateProgress.Value = v));
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Write($"Update failed: {ex}");
            AppState.Current.ErrorMessage = $"Update failed: {ex.Message}";
            ShowUpdate(_update);
        }
    }

    void OnUpdatePage(object sender, RoutedEventArgs e) => Links.Open(_update?.PageUrl ?? Links.Releases);

    void OnUpdateLater(object sender, RoutedEventArgs e) => HideUpdatePanel();

    void OnUpdateSkip(object sender, RoutedEventArgs e)
    {
        if (_update != null)
        {
            AppState.Current.Settings.SkippedUpdateVersion = _update.Tag;
            AppState.Current.Settings.Save();
        }
        HideUpdatePanel();
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
