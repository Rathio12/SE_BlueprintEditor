using System.Globalization;
using System.Text;
using System.Windows.Media;
using SEBlueprint.App.Converters;
using SEBlueprint.App.Services;
using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.App.ViewModels;

public sealed record StatCard(string Title, string Value, string? Subtitle, LimitCheck? Check)
{
    public bool HasLimit => Check?.Limit is > 0;
    public double Percent => Math.Min(100, (Check?.Ratio ?? 0) * 100);
    public double Ratio => Check?.Ratio ?? 0;
    public string LimitText => HasLimit ? $"/ {Format.Unit(Check!.Limit!.Value, Check.Stat == "Cargo (L)" ? "liters" : "int")}" : "";
    public Brush Brush => Check is null or { Status: LimitStatus.Unlimited } ? StatusColors.Accent : StatusColors.For(Check.Status);
}

public sealed record CostLine(string Id, string Name, double Amount, string AmountText, ImageSource? Icon);

public sealed record CheckLine(string Stat, string Value, string Limit, LimitStatus Status)
{
    public Brush Brush => StatusColors.For(Status);
    public string StatusText => Status switch { LimitStatus.Over => "OVER", LimitStatus.Warn => "NEAR", LimitStatus.Ok => "OK", _ => "—" };
}

public sealed record UnknownLine(string Id, int Count);

public sealed class OverviewViewModel : ObservableObject
{
    readonly AppState _app = AppState.Current;

    public OverviewViewModel()
    {
        Row = _app.Selected;
        Rebuild();
    }

    public LibraryRow? Row { get; }
    public bool HasSelection => Row != null;
    public bool HasReport => Report != null;
    public bool IsFull => Report is { IsPartial: false };
    public bool IsPartial => Report?.IsPartial == true;
    public BlueprintReport? Report => Row?.Entry.Report;
    public string Title => Row?.Name ?? "";
    public string Subtitle { get; private set; } = "";
    public string? Error => Row?.Entry.Error;
    public ImageSource? Thumb => Row?.Thumb;

    public IReadOnlyList<StatCard> Cards { get; private set; } = Array.Empty<StatCard>();

    public IReadOnlyList<StatCard> Primary => Cards.Where(c => c.Title is "BLOCKS" or "PCU" or "GUNS" or "CARGO").ToList();

    public IReadOnlyList<StatCard> Secondary => Cards.Where(c => c.Title is not ("BLOCKS" or "PCU" or "GUNS" or "CARGO")).ToList();
    public int PrimaryColumns => Math.Max(1, Primary.Count);
    public IReadOnlyList<CheckLine> Checks { get; private set; } = Array.Empty<CheckLine>();
    public IReadOnlyList<CostLine> Components { get; private set; } = Array.Empty<CostLine>();
    public IReadOnlyList<CostLine> Ingots { get; private set; } = Array.Empty<CostLine>();
    public IReadOnlyList<CostLine> Ore { get; private set; } = Array.Empty<CostLine>();
    public IReadOnlyList<UnknownLine> Unknown { get; private set; } = Array.Empty<UnknownLine>();
    public IReadOnlyList<string> MissingMods { get; private set; } = Array.Empty<string>();
    public bool HasUnknown => Unknown.Count > 0 || MissingMods.Count > 0;
    public string Badge => Report == null ? "" : Report.IsPartial ? "SE2" : Report.UsesMods ? "MODDED" : "VANILLA";
    public string OverallText { get; private set; } = "";
    public Brush OverallBrush { get; private set; } = StatusColors.Neutral;

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
        var profile = _app.ActiveProfile ?? new LimitProfile();
        if (r == null)
        {
            OnPropertyChanged(null);
            return;
        }

        var checks = LimitEvaluator.Evaluate(r, profile);
        LimitCheck? C(string stat) => checks.FirstOrDefault(c => c.Stat == stat);
        var worst = LimitEvaluator.Worst(checks);
        OverallBrush = StatusColors.For(worst);
        OverallText = worst switch
        {
            LimitStatus.Over => "OVER LIMIT",
            LimitStatus.Warn => "NEAR LIMIT",
            LimitStatus.Ok => "WITHIN LIMITS",
            _ => "NO LIMITS",
        };

        var grids = r.Grids.Count == 0 ? "" : string.Join(", ", r.Grids.GroupBy(g => g.GridSize).Select(g => $"{g.Count()} {g.Key.ToLowerInvariant()} grid{(g.Count() == 1 ? "" : "s")}"));
        Subtitle = r.IsPartial ? "Space Engineers 2" : $"{grids}  ·  profile: {profile.Name}";

        var cards = new List<StatCard>
        {
            new("BLOCKS", Format.Unit(r.Blocks, "int"), r.Grids.Count > 1 ? $"largest grid {Format.Unit(r.Grids.Max(g => g.Blocks), "int")}" : null, C("Blocks")),
            new("PCU", Format.Unit(r.Pcu, "int"), null, C("PCU")),
        };
        if (!r.IsPartial)
        {
            cards.AddRange(new[]
            {
                new StatCard("GUNS", Format.Unit(r.Guns, "int"), $"{r.FixedWeapons} fixed, {r.Turrets} turrets", C("Guns")),
                new StatCard("TURRETS", Format.Unit(r.Turrets, "int"), null, C("Turrets")),
                new StatCard("CARGO", Format.Unit(r.CargoLiters, "liters"), $"{r.CargoContainers} containers", C("Cargo (L)")),
                new StatCard("MASS", Format.Unit(r.MassKg, "mass"), "empty", null),
                new StatCard("THRUST", Format.Unit(r.MaxThrustN, "newtons"), $"{r.Thrusters} thrusters, strongest side", null),
                new StatCard("POWER", Format.Unit(r.PowerOutputMW, "mw"), $"{Format.Unit(r.PowerStorageMWh, "mwh")} stored", null),
                new StatCard("JUMP", r.JumpDrives == 0 ? "—" : Format.Unit(r.JumpRangeKm, "km"), r.JumpDrives == 0 ? null : $"{r.JumpDrives} jump drives", null),
            });
        }
        Cards = cards;

        Checks = checks
            .Where(c => c.Status != LimitStatus.Unlimited)
            .Where(c => !r.IsPartial || c.Stat is "PCU" or "Blocks")
            .Select(c => new CheckLine(c.Stat, Format.Unit(c.Value, c.Stat == "Cargo (L)" ? "liters" : "int"),
                Format.Unit(c.Limit ?? 0, c.Stat == "Cargo (L)" ? "liters" : "int"), c.Status))
            .ToList();

        if (!r.IsPartial) BuildCost(r);
        Unknown = r.UnknownBlocks.OrderByDescending(k => k.Value).Select(k => new UnknownLine(k.Key, k.Value)).ToList();
        MissingMods = r.Mods.Where(m => r.MissingMods.Contains(m.Id)).Select(m => m.Name == m.Id ? m.Id : $"{m.Name} ({m.Id})").ToList();
        OnPropertyChanged(null);
    }

    void BuildCost(BlueprintReport r)
    {
        var db = _app.Db;
        var mods = r.Mods.Select(m => m.Id).ToList();
        var calc = new CostCalculator { AssemblerEfficiency = _app.Settings.AssemblerEfficiency, YieldMultiplier = _app.Settings.YieldMultiplier };
        var (ingots, ore) = calc.Compute(r.Components, db, mods);

        CostLine Line(string id, double amount, string kind)
        {
            var item = db.ResolveItem(id, mods);
            var modId = item == null ? null : FindItemMod(db, id, mods);
            return new CostLine(id, db.ItemName(id), amount, Format.Unit(amount, kind), _app.Icons.Get(item?.Icon, modId));
        }

        Components = r.Components.OrderByDescending(k => k.Value).Select(k => Line(k.Key, k.Value, "int")).ToList();
        Ingots = ingots.OrderByDescending(k => k.Value).Select(k => Line(k.Key, k.Value, "mass")).ToList();
        Ore = ore.OrderByDescending(k => k.Value).Select(k => Line(k.Key, k.Value, "mass")).ToList();
    }

    static string? FindItemMod(GameDatabase db, string id, IReadOnlyList<string> mods)
    {
        foreach (var m in mods)
            if (db.ModItems.TryGetValue(m, out var map) && map.ContainsKey(id)) return m;
        if (db.Items.ContainsKey(id)) return null;
        return db.ModItems.FirstOrDefault(kv => kv.Value.ContainsKey(id)).Key;
    }

    public string CostAsText(char sep)
    {
        var sb = new StringBuilder();
        sb.Append("Type").Append(sep).Append("Item").Append(sep).Append("Amount").AppendLine();
        void Add(string type, IEnumerable<CostLine> lines)
        {
            foreach (var l in lines)
                sb.Append(type).Append(sep).Append(Escape(l.Name, sep)).Append(sep)
                  .Append(l.Amount.ToString("0.##", CultureInfo.InvariantCulture)).AppendLine();
        }
        Add("Component", Components);
        Add("Ingot (kg)", Ingots);
        Add("Ore (kg)", Ore);
        return sb.ToString();
    }

    static string Escape(string s, char sep) => s.Contains(sep) || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
}
