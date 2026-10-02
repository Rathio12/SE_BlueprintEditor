using SEBlueprint.Core.Analysis;

namespace SEBlueprint.Core.Limits;

public enum LimitStatus { Unlimited, Ok, Warn, Over }

public sealed record LimitCheck(string Stat, double Value, double? Limit, LimitStatus Status, bool IsGroup = false)
{
    public double Ratio => Limit is > 0 ? Value / Limit.Value : 0;
}

public static class LimitEvaluator
{
    public const double WarnAt = 0.9;

    public static LimitStatus StatusFor(double value, double? limit)
    {
        if (limit is null or <= 0) return LimitStatus.Unlimited;
        if (value > limit) return LimitStatus.Over;
        return value >= limit * WarnAt ? LimitStatus.Warn : LimitStatus.Ok;
    }

    public static IReadOnlyList<LimitCheck> Evaluate(BlueprintReport r, LimitProfile p)
    {
        var checks = new List<LimitCheck>();
        void Add(string stat, double value, double? limit) => checks.Add(new LimitCheck(stat, value, limit, StatusFor(value, limit)));

        Add("PCU", r.Pcu, p.TotalPcu);
        Add("Blocks", r.Blocks, p.MaxBlocksTotal);
        Add("Largest grid", r.Grids.Select(g => g.Blocks).DefaultIfEmpty(0).Max(), p.MaxBlocksPerGrid);
        Add("Largest grid PCU", r.Grids.Select(g => g.Pcu).DefaultIfEmpty(0).Max(), p.MaxPcuPerGrid);
        Add("Guns", r.Guns, p.MaxGuns);
        Add("Turrets", r.Turrets, p.MaxTurrets);
        Add("Cargo (L)", r.CargoLiters, p.MaxCargoLiters);
        foreach (var (key, limit) in p.BlockTypeLimits.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            Add($"Type: {key}", r.BlockPairCounts.GetValueOrDefault(key), limit);
        foreach (var g in p.GroupLimits)
        {
            if (g.Blocks.Count == 0 || g.Max < 0) continue;
            var perGrid = r.Grids.Select(grid => grid.BlockIds.Where(k => BlockMatcher.Matches(k.Key, g.Blocks)).Sum(k => k.Value)).ToList();
            var value = g.Scope == LimitScope.Grid ? perGrid.DefaultIfEmpty(0).Max() : perGrid.Sum();
            var status = g.Max == 0 ? (value > 0 ? LimitStatus.Over : LimitStatus.Ok) : StatusFor(value, g.Max);
            checks.Add(new LimitCheck(g.Stat, value, g.Max, status, IsGroup: true));
        }
        return checks;
    }

    public static LimitStatus Worst(IEnumerable<LimitCheck> checks) =>
        checks.Select(c => c.Status).DefaultIfEmpty(LimitStatus.Unlimited).Max();
}
