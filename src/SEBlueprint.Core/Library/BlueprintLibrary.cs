using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Blueprints;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Games;
using SEBlueprint.Core.Paths;

namespace SEBlueprint.Core.Library;

public sealed class BlueprintEntry
{
    public GameId Game { get; init; } = GameId.SE1;
    public string Folder { get; init; } = "";
    public string Source { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string? ThumbPath { get; init; }
    public DateTime Modified { get; init; }
    public BlueprintReport? Report { get; set; }
    public string? Error { get; set; }

    public string? BlueprintFile => File.Exists(Path.Combine(Folder, "bp.sbc")) ? Path.Combine(Folder, "bp.sbc") : null;
}

public static class BlueprintLibrary
{
    public const string BinaryOnlyError = "Binary blueprint (bp.sbcB5) only — open it in the game once so it saves a bp.sbc.";
    const int MaxDepth = 3;

    public static IReadOnlyList<BlueprintEntry> Enumerate(IEnumerable<BlueprintRoot> roots)
    {
        var list = new List<BlueprintEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
            Walk(root.Path, root.Source, 0, list, seen);
        return list;
    }

    static void Walk(string dir, string source, int depth, List<BlueprintEntry> list, HashSet<string> seen)
    {
        try
        {
            if (!Directory.Exists(dir)) return;
            if (depth > 0 && TryEntry(dir, source, out var entry))
            {
                if (seen.Add(Path.GetFullPath(dir))) list.Add(entry);
                return;
            }
            if (depth >= MaxDepth) return;
            foreach (var sub in Directory.EnumerateDirectories(dir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                Walk(sub, source, depth + 1, list, seen);
        }
        catch (Exception ex) { Log.Write($"Cannot scan {dir}: {ex.Message}"); }
    }

    static bool TryEntry(string dir, string source, out BlueprintEntry entry)
    {
        entry = null!;
        var sbc = Path.Combine(dir, "bp.sbc");
        var b5 = Path.Combine(dir, "bp.sbcB5");
        var hasSbc = File.Exists(sbc);
        if (!hasSbc && !File.Exists(b5)) return false;

        var thumb = Path.Combine(dir, "thumb.png");
        entry = new BlueprintEntry
        {
            Folder = dir,
            Source = source,
            Name = Path.GetFileName(dir),
            ThumbPath = File.Exists(thumb) && new FileInfo(thumb).Length > 0 ? thumb : null,
            Modified = File.GetLastWriteTime(hasSbc ? sbc : b5),
            Error = hasSbc ? null : BinaryOnlyError,
        };
        return true;
    }

    public static async Task AnalyzeAllAsync(IReadOnlyList<BlueprintEntry> entries, GameDatabase db, IProgress<int>? progress, CancellationToken ct)
    {
        var done = 0;
        try
        {
            await Parallel.ForEachAsync(entries, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct }, (entry, token) =>
            {
                Analyze(entry, db);
                progress?.Report(Interlocked.Increment(ref done));
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    public static void Analyze(BlueprintEntry entry, GameDatabase db, CostCalculator? cost = null)
    {
        var file = entry.BlueprintFile;
        if (file == null) return;
        try
        {
            entry.Report = BlueprintAnalyzer.Analyze(BlueprintData.Load(file), db, cost);
            entry.Error = null;
        }
        catch (Exception ex)
        {
            entry.Report = null;
            entry.Error = ex.Message;
            Log.Write($"Cannot analyze {file}: {ex.Message}");
        }
    }
}
