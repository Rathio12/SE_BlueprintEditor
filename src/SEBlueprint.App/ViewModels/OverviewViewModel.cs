using System.Windows.Media;
using SEBlueprint.App.Converters;
using SEBlueprint.App.Services;
using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Presentation;

namespace SEBlueprint.App.ViewModels;

public sealed record StatCard(Readout Source)
{
    public string Title => Source.Title;
    public string Value => Source.Value;
    public string? Subtitle => Source.Subtitle;
    public bool HasLimit => Source.HasLimit;
    public double Ratio => Source.Ratio;
    public double Percent => Math.Min(100, Source.Ratio * 100);
    public string LimitText => Source.LimitText;
    public Brush Brush => StatusColors.For(Source.Tone);
}

public sealed record CostLine(CostRow Source, ImageSource? Icon)
{
    public string Name => Source.Name;
    public string AmountText => Source.AmountText;
}

public sealed record CheckLine(CheckRow Source)
{
    public string Stat => Source.Stat;
    public string Value => Source.Value;
    public string Limit => Source.Limit;
    public string StatusText => Source.StatusText;
    public Brush Brush => StatusColors.For(Source.Tone);
}

public sealed class OverviewViewModel : ObservableObject
{
    readonly AppState _app = AppState.Current;
    ImageSource? _hero;
    ReportView? _view;

    public OverviewViewModel()
    {
        Row = _app.Selected;
        if (Row != null)
        {
            Row.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(LibraryRow.Thumb) && _hero == null) AppState.RunOnUi(() => OnPropertyChanged(nameof(Thumb))); };
            Row.LoadLargeAsync().ContinueWith(t =>
            {
                if (t.Result == null) return;
                AppState.RunOnUi(() =>
                {
                    _hero = t.Result;
                    OnPropertyChanged(nameof(Thumb));
                });
            }, TaskScheduler.Default);
        }
        Rebuild();
    }

    public LibraryRow? Row { get; }
    public bool HasSelection => Row != null;
    public BlueprintReport? Report => Row?.Entry.Report;
    public bool HasReport => Report != null;
    public bool IsFull => Report is { IsPartial: false };
    public bool IsPartial => Report?.IsPartial == true;
    public string Title => Row?.Name ?? "";
    public string? Error => Row?.Entry.Error;
    public ImageSource? Thumb => _hero ?? Row?.Thumb;

    public string Subtitle => _view?.Subtitle ?? "";
    public string Badge => _view?.Badge ?? "";
    public string OverallText => _view?.OverallText ?? "";
    public Brush OverallBrush => StatusColors.For(_view?.OverallTone ?? Tone.Neutral);
    public string? IncompleteText => _view?.IncompleteText;

    public IReadOnlyList<StatCard> Primary { get; private set; } = Array.Empty<StatCard>();
    public IReadOnlyList<StatCard> Secondary { get; private set; } = Array.Empty<StatCard>();
    public IReadOnlyList<CheckLine> Checks { get; private set; } = Array.Empty<CheckLine>();
    public IReadOnlyList<CostLine> Components { get; private set; } = Array.Empty<CostLine>();
    public IReadOnlyList<CostLine> Ingots { get; private set; } = Array.Empty<CostLine>();
    public IReadOnlyList<CostLine> Ore { get; private set; } = Array.Empty<CostLine>();
    public IReadOnlyList<UnknownRow> Unknown => _view?.Unknown ?? Array.Empty<UnknownRow>();
    public IReadOnlyList<string> MissingMods => _view?.MissingMods ?? Array.Empty<string>();
    public bool HasUnknown => Unknown.Count > 0 || MissingMods.Count > 0;

    public int AssemblerIndex
    {
        get => _app.Settings.AssemblerEfficiency switch { >= 10 => 2, >= 3 => 1, _ => 0 };
        set
        {
            _app.Settings.AssemblerEfficiency = value switch { 2 => 10, 1 => 3, _ => 1 };
            _app.Settings.Save();
            Rebuild();
        }
    }

    public int YieldModules
    {
        get => _app.Settings.YieldModules;
        set
        {
            _app.Settings.YieldModules = Math.Clamp(value, 0, 4);
            _app.Settings.Save();
            Rebuild();
        }
    }

    public void Rebuild()
    {
        var r = Report;
        _view = r == null
            ? null
            : ReportView.Build(r, _app.ActiveProfile ?? new LimitProfile(), _app.Db, _app.Settings.AssemblerEfficiency, _app.Settings.YieldMultiplier);

        Primary = _view?.Primary.Select(p => new StatCard(p)).ToList() ?? (IReadOnlyList<StatCard>)Array.Empty<StatCard>();
        Secondary = _view?.Secondary.Select(p => new StatCard(p)).ToList() ?? (IReadOnlyList<StatCard>)Array.Empty<StatCard>();
        Checks = _view?.Checks.Select(c => new CheckLine(c)).ToList() ?? (IReadOnlyList<CheckLine>)Array.Empty<CheckLine>();
        Components = Lines(_view?.Components);
        Ingots = Lines(_view?.Ingots);
        Ore = Lines(_view?.Ore);
        OnPropertyChanged(null);
    }

    IReadOnlyList<CostLine> Lines(IReadOnlyList<CostRow>? rows) =>
        rows?.Select(c => new CostLine(c, _app.Icons.Get(c.Icon, c.ModId))).ToList() ?? (IReadOnlyList<CostLine>)Array.Empty<CostLine>();

    public string CostAsText(char separator) => _view?.CostAsText(separator) ?? "";
}
