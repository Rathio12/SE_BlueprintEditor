using SEBlueprint.Core.Analysis;

namespace SEBlueprint.Core.Limits;

public enum LimitStatus { Unlimited, Ok, Warn, Over }

public sealed record LimitCheck(string Stat, double Value, double? Limit, LimitStatus Status)
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
        Add("Guns", r.Guns, p.MaxGuns);
        Add("Turrets", r.Turrets, p.MaxTurrets);
        Add("Cargo (L)", r.CargoLiters, p.MaxCargoLiters);
        foreach (var (key, limit) in p.BlockTypeLimits.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            Add($"Type: {key}", r.BlockPairCounts.GetValueOrDefault(key), limit);
        return checks;
    }

    public static LimitStatus Worst(IEnumerable<LimitCheck> checks) =>
        checks.Select(c => c.Status).DefaultIfEmpty(LimitStatus.Unlimited).Max();
}
