using SEBlueprint.Core.Library;

namespace SEBlueprint.Core.Games;

public enum GameId { SE1, SE2 }

public interface IGameAdapter
{
    GameId Game { get; }
    bool IsInstalled { get; }

    bool CanAnalyze { get; }
    IReadOnlyList<BlueprintEntry> Enumerate();
}
