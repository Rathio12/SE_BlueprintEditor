namespace SEBlueprint.Core.Paths;

public sealed record BlueprintRoot(string Path, string Source);

public sealed record GamePaths(string? GameDir, string? WorkshopDir, IReadOnlyList<BlueprintRoot> BlueprintRoots)
{
    public string? DataDir => GameDir is null ? null : Path.Combine(GameDir, "Content", "Data");
    public string? CustomWorldsDir => GameDir is null ? null : Path.Combine(GameDir, "Content", "CustomWorlds");

    public string? Se2GameDir { get; init; }

    public string? Se2BlueprintDir { get; init; }
}
