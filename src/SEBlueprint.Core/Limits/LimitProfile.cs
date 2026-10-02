namespace SEBlueprint.Core.Limits;

/// <summary>A set of limits to check blueprints against (a vanilla world preset, a server, or your own rules). Null or 0 means unlimited.</summary>
public sealed class LimitProfile
{
    public string Name { get; set; } = "";
    public bool BuiltIn { get; set; }
    public int? TotalPcu { get; set; }
    public int? MaxBlocksPerGrid { get; set; }
    public int? MaxBlocksTotal { get; set; }
    public int? MaxGuns { get; set; }
    public int? MaxTurrets { get; set; }
    public double? MaxCargoLiters { get; set; }
    /// <summary>Block pair name (as in the game's BlockTypeLimits) → maximum count.</summary>
    public Dictionary<string, int> BlockTypeLimits { get; set; } = new();

    public LimitProfile Clone(string? name = null) => new()
    {
        Name = name ?? Name,
        BuiltIn = false,
        TotalPcu = TotalPcu,
        MaxBlocksPerGrid = MaxBlocksPerGrid,
        MaxBlocksTotal = MaxBlocksTotal,
        MaxGuns = MaxGuns,
        MaxTurrets = MaxTurrets,
        MaxCargoLiters = MaxCargoLiters,
        BlockTypeLimits = new(BlockTypeLimits),
    };

    /// <summary>A string that is equal for two profiles with the same limits (ignores the name).</summary>
    internal string LimitsKey() =>
        $"{TotalPcu}|{MaxBlocksPerGrid}|{MaxBlocksTotal}|{MaxGuns}|{MaxTurrets}|{MaxCargoLiters}|" +
        string.Join(",", BlockTypeLimits.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}"));
}
