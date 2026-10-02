using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SEBlueprint.Core.Games;
using SEBlueprint.Core.Library;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.App.ViewModels;

public enum RowStatus { Pending, Ok, Warn, Over, Unlimited, Error }

public sealed class LibraryRow : ObservableObject
{
    ImageSource? _thumb;
    bool _thumbLoaded;
    RowStatus _status = RowStatus.Pending;
    string _statusText = "…";

    public LibraryRow(BlueprintEntry entry) => Entry = entry;

    public BlueprintEntry Entry { get; }

    public string Name => string.IsNullOrWhiteSpace(Entry.Report?.Name) ? Entry.Name : Entry.Report!.Name;
    public string Game => Entry.Game == GameId.SE2 ? "SE2" : "SE1";
    public string Source => Entry.Source;
    public DateTime Modified => Entry.Modified;
    public bool IsAnalyzed => Entry.Report != null;
    public bool IsPartial => Entry.Report?.IsPartial == true;
    public int? Blocks => Entry.Report?.Blocks;
    public int? Pcu => Entry.Report?.Pcu;
    public int? Guns => Full?.Guns;
    public int? Turrets => Full?.Turrets;
    public double? CargoLiters => Full?.CargoLiters;
    public bool UsesMods => Full?.UsesMods == true;
    public string ModdedTag => UsesMods ? "· MODDED" : "";
    public int UnknownCount => Full?.UnknownBlocks.Values.Sum() ?? 0;

    Core.Analysis.BlueprintReport? Full => Entry.Report is { IsPartial: false } r ? r : null;

    public RowStatus Status { get => _status; private set => Set(ref _status, value); }
    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }

    public ImageSource? Thumb
    {
        get
        {
            if (!_thumbLoaded)
            {
                _thumbLoaded = true;
                Task.Run(LoadThumb).ContinueWith(t =>
                {
                    if (t.Result == null) return;
                    _thumb = t.Result;
                    OnPropertyChanged(nameof(Thumb));
                }, TaskScheduler.Default);
            }
            return _thumb;
        }
    }

    ImageSource? LoadThumb()
    {
        var path = Entry.ThumbPath;
        if (path == null)
        {
            foreach (var name in new[] { "thumb.png", "thumb.jpg", "preview.png" })
            {
                var p = Path.Combine(Entry.Folder, name);
                if (File.Exists(p)) { path = p; break; }
            }
        }
        if (path == null) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.DecodePixelWidth = 160;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch (Exception) { return null; }
    }

    public void Refresh(LimitProfile? profile)
    {
        if (Entry.Error != null) { Status = RowStatus.Error; StatusText = "Error"; }
        else if (Entry.Report == null) { Status = RowStatus.Pending; StatusText = "…"; }
        else
        {
            var worst = profile == null ? LimitStatus.Unlimited : LimitEvaluator.Worst(LimitEvaluator.Evaluate(Entry.Report, profile));
            (Status, StatusText) = worst switch
            {
                LimitStatus.Over => (RowStatus.Over, "Over limit"),
                LimitStatus.Warn => (RowStatus.Warn, "Near limit"),
                LimitStatus.Ok => (RowStatus.Ok, "OK"),
                _ => (RowStatus.Unlimited, "No limits"),
            };
        }
        OnPropertyChanged(null);
    }
}
