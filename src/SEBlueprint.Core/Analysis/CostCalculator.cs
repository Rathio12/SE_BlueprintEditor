using SEBlueprint.Core.Data;

namespace SEBlueprint.Core.Analysis;

/// <summary>Turns a component list into the ingots and ore needed to build it.</summary>
public sealed class CostCalculator
{
    const int MaxDepth = 16;

    /// <summary>Refinery yield (1 = no modules, 2 = four yield modules).</summary>
    public double YieldMultiplier { get; set; } = 1;
    /// <summary>World assembler efficiency (1 = realistic, 3 = x3, 10 = x10).</summary>
    public double AssemblerEfficiency { get; set; } = 1;

    public (Dictionary<string, double> Ingots, Dictionary<string, double> Ore) Compute(
        IReadOnlyDictionary<string, int> components, GameDatabase db, IReadOnlyList<string> enabledMods)
    {
        var ingots = new Dictionary<string, double>();
        var assembler = AssemblerEfficiency > 0 ? AssemblerEfficiency : 1;
        foreach (var (id, count) in components)
            Expand(id, count, 0);

        var ore = new Dictionary<string, double>();
        var yield = YieldMultiplier > 0 ? YieldMultiplier : 1;
        foreach (var (id, amount) in ingots)
        {
            var recipe = db.ResolveRecipe(id, enabledMods);
            if (recipe == null) continue;
            foreach (var (input, perUnit) in recipe)
                Add(ore, input, amount * perUnit / yield);
        }
        return (ingots, ore);

        void Expand(string id, double count, int depth)
        {
            var recipe = depth < MaxDepth ? db.ResolveRecipe(id, enabledMods) : null;
            if (recipe == null)
            {
                // Raw material with no known recipe (or a recipe loop): count it as-is.
                if (!id.StartsWith("Component/", StringComparison.Ordinal)) Add(ingots, id, count);
                return;
            }
            foreach (var (input, perUnit) in recipe)
            {
                if (input.StartsWith("Component/", StringComparison.Ordinal))
                    Expand(input, count * perUnit, depth + 1);
                else
                    Add(ingots, input, count * perUnit / assembler);
            }
        }
    }

    static void Add(Dictionary<string, double> d, string key, double value) => d[key] = d.GetValueOrDefault(key) + value;
}
