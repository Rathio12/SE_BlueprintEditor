using System.Globalization;
using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.Core.Presentation;

public enum Tone { Accent, Ok, Warn, Over, Neutral }

public sealed record Readout(string Title, string Value, string? Subtitle, LimitCheck? Check)
{
    public bool HasLimit => Check?.Limit is > 0;
    public double Ratio => Check?.Ratio ?? 0;
    public string LimitText => HasLimit ? "/ " + Units.Format(Check!.Limit!.Value, Check.Stat == "Cargo (L)" ? "liters" : "int") : "";
    public Tone Tone => Check is null || Check.Status == LimitStatus.Unlimited ? Tone.Accent : ReportView.ToneOf(Check.Status);
}

public sealed record CheckRow(string Stat, string Value, string Limit, LimitStatus Status)
{
    public Tone Tone => ReportView.ToneOf(Status);
    public string StatusText => Status switch { LimitStatus.Over => "OVER", LimitStatus.Warn => "NEAR", LimitStatus.Ok => "OK", _ => "—" };
}

public sealed record CostRow(string Id, string Name, double Amount, string AmountText, string? Icon, string? ModId);

public sealed record UnknownRow(string Id, int Count);

public sealed class ReportView
{
    public IReadOnlyList<Readout> Primary { get; private init; } = Array.Empty<Readout>();
    public IReadOnlyList<Readout> Secondary { get; private init; } = Array.Empty<Readout>();
    public IReadOnlyList<CheckRow> Checks { get; private init; } = Array.Empty<CheckRow>();
    public IReadOnlyList<CostRow> Components { get; private init; } = Array.Empty<CostRow>();
    public IReadOnlyList<CostRow> Ingots { get; private init; } = Array.Empty<CostRow>();
    public IReadOnlyList<CostRow> Ore { get; private init; } = Array.Empty<CostRow>();
    public IReadOnlyList<UnknownRow> Unknown { get; private init; } = Array.Empty<UnknownRow>();
    public IReadOnlyList<string> MissingMods { get; private init; } = Array.Empty<string>();
    public LimitStatus Overall { get; private init; }
    public string OverallText { get; private init; } = "";
    public Tone OverallTone { get; private init; } = Tone.Neutral;
    public string? IncompleteText { get; private init; }
    public string Badge { get; private init; } = "";
    public string Subtitle { get; private init; } = "";

    public static Tone ToneOf(LimitStatus s) => s switch
    {
        LimitStatus.Ok => Tone.Ok,
        LimitStatus.Warn => Tone.Warn,
        LimitStatus.Over => Tone.Over,
        _ => Tone.Neutral,
    };

    public static ReportView Build(BlueprintReport r, LimitProfile profile, GameDatabase db, double assemblerEfficiency = 1, double yieldMultiplier = 1)
    {
        var checks = LimitEvaluator.Evaluate(r, profile);
        LimitCheck? C(string stat) => checks.FirstOrDefault(c => c.Stat == stat);
        LimitCheck? Either(string total, string perGrid) =>
            C(total) is { Limit: > 0 } t ? t : C(perGrid) is { Limit: > 0 } g ? g : C(total);
        var worst = LimitEvaluator.Worst(checks);

        var primary = new List<Readout>
        {
            new("BLOCKS", Units.Format(r.Blocks, "int"), r.Grids.Count > 1 ? $"largest grid {Units.Format(r.Grids.Max(g => g.Blocks), "int")}" : null, Either("Blocks", "Largest grid")),
            new("PCU", Units.Format(r.Pcu, "int"), r.Grids.Count > 1 ? $"largest grid {Units.Format(r.Grids.Max(g => g.Pcu), "int")}" : null, Either("PCU", "Largest grid PCU")),
        };
        var secondary = new List<Readout>();
        if (!r.IsPartial)
        {
            primary.Add(new("GUNS", Units.Format(r.Guns, "int"), $"{r.FixedWeapons} fixed, {r.Turrets} turrets", C("Guns")));
            primary.Add(new("CARGO", Units.Format(r.CargoLiters, "liters"), $"{r.CargoContainers} containers", C("Cargo (L)")));
            secondary.Add(new("TURRETS", Units.Format(r.Turrets, "int"), null, C("Turrets")));
            secondary.Add(new("MASS", Units.Format(r.MassKg, "mass"), "empty", null));
            secondary.Add(new("THRUST", Units.Format(r.MaxThrustN, "newtons"), $"{r.Thrusters} thrusters, strongest side", null));
            secondary.Add(new("POWER", Units.Format(r.PowerOutputMW, "mw"), $"{Units.Format(r.PowerStorageMWh, "mwh")} stored", null));
            secondary.Add(new("JUMP", r.JumpDrives == 0 ? "—" : Units.Format(r.JumpRangeKm, "km"), r.JumpDrives == 0 ? null : $"{r.JumpDrives} jump drives", null));
        }

        var checkRows = checks
            .Where(c => c.Status != LimitStatus.Unlimited)
            .Where(c => !r.IsPartial || c.Stat is "PCU" or "Blocks")
            .Select(c =>
            {
                var kind = c.Stat == "Cargo (L)" ? "liters" : "int";
                return new CheckRow(c.Stat, Units.Format(c.Value, kind), Units.Format(c.Limit ?? 0, kind), c.Status);
            })
            .ToList();

        var mods = r.Mods.Select(m => m.Id).ToList();
        IReadOnlyList<CostRow> components = Array.Empty<CostRow>(), ingots = Array.Empty<CostRow>(), ore = Array.Empty<CostRow>();
        if (!r.IsPartial)
        {
            var calc = new CostCalculator { AssemblerEfficiency = assemblerEfficiency, YieldMultiplier = yieldMultiplier };
            var (ing, or) = calc.Compute(r.Components, db, mods);
            CostRow Row(string id, double amount, string kind)
            {
                var item = db.ResolveItem(id, mods);
                return new CostRow(id, db.ItemName(id), amount, Units.Format(amount, kind), item?.Icon, FindItemMod(db, id, mods));
            }
            components = r.Components.OrderByDescending(k => k.Value).Select(k => Row(k.Key, k.Value, "int")).ToList();
            ingots = ing.OrderByDescending(k => k.Value).Select(k => Row(k.Key, k.Value, "mass")).ToList();
            ore = or.OrderByDescending(k => k.Value).Select(k => Row(k.Key, k.Value, "mass")).ToList();
        }

        var grids = string.Join(", ", r.Grids.GroupBy(g => g.GridSize)
            .Select(g => $"{g.Count()} {g.Key.ToLowerInvariant()} grid{(g.Count() == 1 ? "" : "s")}"));

        return new ReportView
        {
            Primary = primary,
            Secondary = secondary,
            Checks = checkRows,
            Components = components,
            Ingots = ingots,
            Ore = ore,
            Unknown = r.UnknownBlocks.OrderByDescending(k => k.Value).Select(k => new UnknownRow(k.Key, k.Value)).ToList(),
            MissingMods = r.Mods.Where(m => r.MissingMods.Contains(m.Id)).Select(m => m.Name == m.Id ? m.Id : $"{m.Name} ({m.Id})").ToList(),
            Overall = worst,
            OverallText = worst == LimitStatus.Over ? "OVER LIMIT"
                : r.IsIncomplete ? "INCOMPLETE — CHECK MODS"
                : worst switch
                {
                    LimitStatus.Warn => "NEAR LIMIT",
                    LimitStatus.Ok => "WITHIN LIMITS",
                    _ => "NO LIMITS",
                },
            OverallTone = worst == LimitStatus.Over ? Tone.Over : r.IsIncomplete ? Tone.Warn : ToneOf(worst),
            IncompleteText = r.IsIncomplete
                ? $"{Units.Format(r.UnknownBlockCount, "int")} UNKNOWN BLOCK{(r.UnknownBlockCount == 1 ? "" : "S")} — COUNTS INCOMPLETE"
                : null,
            Badge = r.IsPartial ? "SE2" : r.UsesMods ? "MODDED" : "VANILLA",
            Subtitle = r.IsPartial ? "Space Engineers 2  ·  full support coming soon" : $"{grids}  ·  profile: {profile.Name}",
        };
    }

    public string CostAsText(char separator)
    {
        var lines = new List<string> { string.Join(separator, "Type", "Item", "Amount") };
        void Add(string type, IEnumerable<CostRow> rows)
        {
            foreach (var row in rows)
                lines.Add(string.Join(separator, type, Escape(row.Name, separator), row.Amount.ToString("0.##", CultureInfo.InvariantCulture)));
        }
        Add("Component", Components);
        Add("Ingot (kg)", Ingots);
        Add("Ore (kg)", Ore);
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    static string Escape(string s, char sep) => s.Contains(sep) || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    static string? FindItemMod(GameDatabase db, string id, IReadOnlyList<string> mods)
    {
        foreach (var m in mods)
            if (db.ModItems.TryGetValue(m, out var map) && map.ContainsKey(id)) return m;
        if (db.Items.ContainsKey(id)) return null;
        return db.ModItems.FirstOrDefault(kv => kv.Value.ContainsKey(id)).Key;
    }
}

public static class Units
{
    public static string Format(double v, string kind)
    {
        var c = CultureInfo.CurrentCulture;
        return kind switch
        {
            "liters" => v >= 1_000_000 ? $"{(v / 1_000_000).ToString("0.##", c)} ML" : v >= 10_000 ? $"{(v / 1000).ToString("#,0.#", c)} kL" : $"{v.ToString("#,0", c)} L",
            "mass" => v >= 1_000_000 ? $"{(v / 1_000_000).ToString("0.##", c)} kt" : v >= 10_000 ? $"{(v / 1000).ToString("#,0.#", c)} t" : $"{v.ToString("#,0", c)} kg",
            "newtons" => v >= 1_000_000 ? $"{(v / 1_000_000).ToString("0.##", c)} MN" : $"{(v / 1000).ToString("#,0", c)} kN",
            "mw" => $"{v.ToString("#,0.##", c)} MW",
            "mwh" => $"{v.ToString("#,0.##", c)} MWh",
            "km" => $"{v.ToString("#,0.#", c)} km",
            _ => v.ToString("#,0", c),
        };
    }
}
