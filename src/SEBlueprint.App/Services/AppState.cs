using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using SEBlueprint.App.ViewModels;
using SEBlueprint.Core;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Games;
using SEBlueprint.Core.Library;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Paths;

namespace SEBlueprint.App.Services;

public sealed class AppState : ObservableObject
{
    public static AppState Current { get; } = new();

    GamePaths _paths = new(null, null, Array.Empty<BlueprintRoot>());
    GameDatabase _db = new();
    LimitProfile? _activeProfile;
    LibraryRow? _selected;
    string _status = "Starting…";
    bool _isBusy;
    double _progress;
    string? _errorMessage;
    CancellationTokenSource? _scan;

    AppState() { }

    public AppSettings Settings { get; } = AppSettings.Load();
    public GameIconService Icons { get; } = new();
    public ObservableCollection<LibraryRow> Rows { get; } = new();
    public ObservableCollection<LimitProfile> Profiles { get; } = new();

    public GamePaths Paths { get => _paths; private set => Set(ref _paths, value); }
    public GameDatabase Db { get => _db; private set => Set(ref _db, value); }
    public string Status { get => _status; set => Set(ref _status, value); }
    public bool IsBusy { get => _isBusy; set => Set(ref _isBusy, value); }

    public double Progress { get => _progress; set { if (Set(ref _progress, value)) OnPropertyChanged(nameof(ProgressRatio)); } }
    public double ProgressRatio => Progress / 100.0;
    public string? ErrorMessage { get => _errorMessage; set { if (Set(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool GameFound => Paths.GameDir != null;

    public LimitProfile? ActiveProfile
    {
        get => _activeProfile;
        set
        {
            if (_reloadingProfiles || (value == null && Profiles.Count > 0)) return;
            if (!Set(ref _activeProfile, value)) return;
            Settings.ActiveProfileName = value?.Name;
            Settings.Save();
            foreach (var r in Rows) r.Refresh(value);
        }
    }

    bool _reloadingProfiles;

    public LibraryRow? Selected { get => _selected; set => Set(ref _selected, value); }

    public static string CacheFile => Path.Combine(Storage.Root, "gamedb.json");

    public async Task InitializeAsync(bool rebuildCache = false)
    {
        _scan?.Cancel();
        var scan = _scan = new CancellationTokenSource();
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            Status = "Looking for Space Engineers…";
            Paths = await Task.Run(DetectPaths);
            OnPropertyChanged(nameof(GameFound));
            Icons.ContentDir = Paths.GameDir == null ? null : Path.Combine(Paths.GameDir, "Content");
            Icons.WorkshopDir = Paths.WorkshopDir;

            LoadProfiles();

            if (rebuildCache) TryDelete(CacheFile);
            var progress = new Progress<string>(s => Status = s);
            Status = "Loading game data…";
            Db = await Task.Run(() => GameDatabaseLoader.LoadCached(Paths.DataDir, Paths.WorkshopDir, CacheFile, progress));

            Status = "Finding blueprints…";
            var entries = await Task.Run(() =>
                new Se1Adapter(Paths).Enumerate().Concat(new Se2Adapter(Paths).Enumerate()).ToList());
            Rows.Clear();
            foreach (var e in entries.OrderByDescending(e => e.Modified)) Rows.Add(new LibraryRow(e));
            foreach (var r in Rows) r.Refresh(ActiveProfile);

            var se1 = entries.Where(e => e.Game == GameId.SE1 && e.Error == null).ToList();
            var pending = Rows.Where(r => r.Entry.Report == null && r.Entry.Error == null).ToList();
            var analyzed = new Progress<int>(n =>
            {
                Progress = se1.Count == 0 ? 100 : n * 100.0 / se1.Count;
                Status = $"Analyzing blueprints… {n}/{se1.Count}";

                for (var i = pending.Count - 1; i >= 0; i--)
                {
                    var row = pending[i];
                    if (row.Entry.Report == null && row.Entry.Error == null) continue;
                    row.Refresh(ActiveProfile);
                    pending.RemoveAt(i);
                }
            });
            await BlueprintLibrary.AnalyzeAllAsync(se1, Db, analyzed, scan.Token);
            if (scan.IsCancellationRequested) return;
            foreach (var r in Rows) r.Refresh(ActiveProfile);

            Status = Paths.GameDir == null
                ? $"{Rows.Count} blueprints · Space Engineers not found — set the folder in Settings"
                : $"{Rows.Count} blueprints · {Db.Blocks.Count:N0} vanilla blocks · {Db.ModBlocks.Count} mods with blocks";
        }
        catch (Exception ex)
        {
            Log.Write($"Initialization failed: {ex}");
            ErrorMessage = $"Something went wrong while loading: {ex.Message}";
            Status = "Ready (with errors)";
        }
        finally
        {
            if (_scan == scan) { IsBusy = false; Progress = 100; }
        }
    }

    GamePaths DetectPaths()
    {
        var p = SteamLocator.Detect();
        var game = ValidDir(Settings.GameDirOverride) ?? p.GameDir;
        var workshop = ValidDir(Settings.WorkshopDirOverride) ?? p.WorkshopDir;
        var roots = p.BlueprintRoots.ToList();
        if (workshop != null && workshop != p.WorkshopDir)
        {
            roots.RemoveAll(r => r.Path == p.WorkshopDir);
            roots.Add(new BlueprintRoot(workshop, "Workshop"));
        }
        foreach (var extra in Settings.ExtraBlueprintRoots)
            if (ValidDir(extra) is { } dir && roots.All(r => !string.Equals(r.Path, dir, StringComparison.OrdinalIgnoreCase)))
                roots.Add(new BlueprintRoot(dir, "Custom"));
        return new GamePaths(game, workshop, roots)
        {
            Se2GameDir = p.Se2GameDir,
            Se2BlueprintDir = ValidDir(Settings.Se2BlueprintDirOverride) ?? p.Se2BlueprintDir,
        };
    }

    public void LoadProfiles()
    {
        var keep = Settings.ActiveProfileName;
        _reloadingProfiles = true;
        try
        {
            Profiles.Clear();
            foreach (var p in VanillaProfiles.Load(Paths.CustomWorldsDir)) Profiles.Add(p);
            foreach (var p in PresetProfiles.All) Profiles.Add(p);
            foreach (var p in ProfileStore.LoadUser()) Profiles.Add(p);
        }
        finally { _reloadingProfiles = false; }
        _activeProfile = null;
        ActiveProfile = Profiles.FirstOrDefault(p => p.Name == keep) ?? Profiles.FirstOrDefault();
    }

    public void Reanalyze(LibraryRow row)
    {
        if (row.Entry.Game != GameId.SE1) return;
        BlueprintLibrary.Analyze(row.Entry, Db);
        row.Refresh(ActiveProfile);
    }

    public static void RunOnUi(Action a)
    {
        var d = Application.Current?.Dispatcher;
        if (d == null || d.CheckAccess()) a();
        else d.BeginInvoke(a);
    }

    static string? ValidDir(string? dir)
    {
        try { return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir) ? dir : null; }
        catch (Exception) { return null; }
    }

    static void TryDelete(string file)
    {
        try { if (File.Exists(file)) File.Delete(file); }
        catch (Exception ex) { Log.Write($"Cannot delete {file}: {ex.Message}"); }
    }
}
