using SEBlueprint.Core.Blueprints;

namespace SEBlueprint.Core.Analysis;

public sealed class GridSummary
{
    public string Name { get; init; } = "";
    public string GridSize { get; init; } = "";
    public int Blocks { get; init; }
    public int Pcu { get; init; }
}

/// <summary>Everything the app shows about one blueprint.</summary>
public sealed class BlueprintReport
{
    public string Name { get; init; } = "";
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
    /// <summary>Thrust in the strongest single direction, in space, in newtons.</summary>
    public double MaxThrustN { get; set; }

    public double PowerOutputMW { get; set; }
    public double PowerStorageMWh { get; set; }
    public int JumpDrives { get; set; }
    public double JumpRangeKm { get; set; }

    public Dictionary<string, int> Components { get; } = new();
    public Dictionary<string, double> Ingots { get; set; } = new();
    public Dictionary<string, double> Ore { get; set; } = new();

    /// <summary>Block pair name (or TypeId when the block has none) → count. Matches the game's BlockTypeLimits keys.</summary>
    public Dictionary<string, int> BlockPairCounts { get; } = new();
    /// <summary>Block ids no installed definition knows about → count.</summary>
    public Dictionary<string, int> UnknownBlocks { get; } = new();

    public List<BlueprintMod> Mods { get; } = new();
    /// <summary>Mod ids the blueprint lists that are not on this PC.</summary>
    public List<string> MissingMods { get; } = new();
    public bool UsesMods { get; set; }
    /// <summary>Blocks resolved from a mod the blueprint did not list.</summary>
    public int BlocksFromUnlistedMods { get; set; }
}
