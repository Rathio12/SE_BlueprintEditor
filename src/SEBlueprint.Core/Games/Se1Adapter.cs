using SEBlueprint.Core.Library;
using SEBlueprint.Core.Paths;

namespace SEBlueprint.Core.Games;

/// <summary>Space Engineers (1): full analysis from bp.sbc files and the local game data.</summary>
public sealed class Se1Adapter(GamePaths paths) : IGameAdapter
{
    public GameId Game => GameId.SE1;
    public bool IsInstalled => paths.GameDir != null;
    public bool CanAnalyze => true;
    public IReadOnlyList<BlueprintEntry> Enumerate() => BlueprintLibrary.Enumerate(paths.BlueprintRoots);
}
