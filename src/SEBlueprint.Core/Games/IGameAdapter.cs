using SEBlueprint.Core.Library;

namespace SEBlueprint.Core.Games;

public enum GameId { SE1, SE2 }

/// <summary>One supported game: where its blueprints are and how much of them can be analyzed.</summary>
public interface IGameAdapter
{
    GameId Game { get; }
    bool IsInstalled { get; }
    /// <summary>True when full analysis (cost, guns, cargo…) is supported.</summary>
    bool CanAnalyze { get; }
    IReadOnlyList<BlueprintEntry> Enumerate();
}
