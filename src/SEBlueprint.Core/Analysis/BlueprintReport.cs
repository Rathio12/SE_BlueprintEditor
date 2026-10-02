using SEBlueprint.Core.Blueprints;

namespace SEBlueprint.Core.Analysis;

public sealed class GridSummary
{
    public string Name { get; init; } = "";
    public string GridSize { get; init; } = "";
    public int Blocks { get; init; }
    public int Pcu { get; init; }
}

public sealed class BlueprintReport
{
    public string Name { get; init; } = "";

    public bool IsPartial { get; init; }
    public List<GridSummary> Grids { get; } = new();

    public int Blocks { get; set; }
    public int Pcu { get; set; }
    public double MassKg { get; set; }

    public int FixedWeapons { get; set; }
    public int Turrets { get; set; }
    public int Guns => FixedWeapons + Turrets;

    public int CargoContainers { get; set; }
    public double CargoLiters { get; set; }

    public int Thrusters { get; set; }

    public double MaxThrustN { get; set; }

    public double PowerOutputMW { get; set; }
    public double PowerStorageMWh { get; set; }
    public int JumpDrives { get; set; }
    public double JumpRangeKm { get; set; }

    public Dictionary<string, int> Components { get; } = new();
    public Dictionary<string, double> Ingots { get; set; } = new();
    public Dictionary<string, double> Ore { get; set; } = new();

    public Dictionary<string, int> BlockPairCounts { get; } = new();

    public Dictionary<string, int> UnknownBlocks { get; } = new();

    public List<BlueprintMod> Mods { get; } = new();

    public List<string> MissingMods { get; } = new();
    public bool UsesMods { get; set; }

    public int BlocksFromUnlistedMods { get; set; }
}
