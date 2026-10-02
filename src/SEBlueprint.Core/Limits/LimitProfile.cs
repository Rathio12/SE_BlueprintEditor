namespace SEBlueprint.Core.Limits;

public sealed class LimitProfile
{
    public string Name { get; set; } = "";
    public bool BuiltIn { get; set; }
    public string? Description { get; set; }
    public int? TotalPcu { get; set; }
    public int? MaxBlocksPerGrid { get; set; }
    public int? MaxPcuPerGrid { get; set; }
    public int? MaxBlocksTotal { get; set; }
    public int? MaxGuns { get; set; }
    public int? MaxTurrets { get; set; }
    public double? MaxCargoLiters { get; set; }

    public Dictionary<string, int> BlockTypeLimits { get; set; } = new();
    public List<BlockGroupLimit> GroupLimits { get; set; } = new();

    public override string ToString() => Name;

    public LimitProfile Clone(string? name = null) => new()
    {
        Name = name ?? Name,
        BuiltIn = false,
        TotalPcu = TotalPcu,
        Description = Description,
        MaxBlocksPerGrid = MaxBlocksPerGrid,
        MaxPcuPerGrid = MaxPcuPerGrid,
        MaxBlocksTotal = MaxBlocksTotal,
        MaxGuns = MaxGuns,
        MaxTurrets = MaxTurrets,
        MaxCargoLiters = MaxCargoLiters,
        BlockTypeLimits = new(BlockTypeLimits),
        GroupLimits = GroupLimits.Select(g => g.Clone()).ToList(),
    };

    internal string LimitsKey() =>
        $"{TotalPcu}|{MaxPcuPerGrid}|{MaxBlocksPerGrid}|{MaxBlocksTotal}|{MaxGuns}|{MaxTurrets}|{MaxCargoLiters}|" +
        string.Join(",", BlockTypeLimits.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}")) + "|" +
        string.Join(",", GroupLimits.Select(g => g.Key()));
}
