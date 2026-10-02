namespace SEBlueprint.Core.Data;

public enum BlockCategory { Other, FixedWeapon, Turret, Cargo, Thruster, Power, JumpDrive }

public sealed class BlockDefinition
{
    /// <summary>"TypeId/SubtypeId" without the MyObjectBuilder_ prefix.</summary>
    public string Id { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore]
    public string TypeId => Id.Split('/')[0];
    [System.Text.Json.Serialization.JsonIgnore]
    public string SubtypeId => Id.Contains('/') ? Id[(Id.IndexOf('/') + 1)..] : "";
    public string DisplayName { get; set; } = "";
    /// <summary>Icon path relative to the game's Content folder (vanilla) or the mod folder, with '/' separators.</summary>
    public string? Icon { get; set; }
    public string? PairName { get; set; }
    public string GridSize { get; set; } = "Large";
    public int SizeX { get; set; } = 1;
    public int SizeY { get; set; } = 1;
    public int SizeZ { get; set; } = 1;
    public int Pcu { get; set; }
    public Dictionary<string, int> Components { get; set; } = new();
    public BlockCategory Category { get; set; }
    public double CargoLiters { get; set; }
    public double ThrustForce { get; set; }
    public double ThrustSpaceEff { get; set; } = 1;
    public double ThrustPlanetEff { get; set; } = 1;
    public double PowerOutput { get; set; }
    public double PowerStorage { get; set; }
    public bool IsGenerator { get; set; }
    public double JumpDistance { get; set; }
    public double JumpMaxMass { get; set; }
    /// <summary>Workshop id of the mod that defines this block; null for vanilla.</summary>
    public string? ModId { get; set; }
}

public sealed class ComponentDefinition
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>Icon path relative to the game's Content folder (vanilla) or the mod folder, with '/' separators.</summary>
    public string? Icon { get; set; }
    public double Mass { get; set; }
    public double Volume { get; set; }
}
