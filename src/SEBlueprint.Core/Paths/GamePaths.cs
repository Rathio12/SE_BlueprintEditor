namespace SEBlueprint.Core.Paths;

/// <summary>A folder that contains blueprint sub-folders. Source is "Local", "Cloud" or "Workshop".</summary>
public sealed record BlueprintRoot(string Path, string Source);

public sealed record GamePaths(string? GameDir, string? WorkshopDir, IReadOnlyList<BlueprintRoot> BlueprintRoots)
{
    public string? DataDir => GameDir is null ? null : Path.Combine(GameDir, "Content", "Data");
    public string? CustomWorldsDir => GameDir is null ? null : Path.Combine(GameDir, "Content", "CustomWorlds");
    /// <summary>Space Engineers 2 install folder, when found.</summary>
    public string? Se2GameDir { get; init; }
    /// <summary>Space Engineers 2 blueprint folder (%AppData%\SpaceEngineers2\AppData\Blueprints), when found.</summary>
    public string? Se2BlueprintDir { get; init; }
}
