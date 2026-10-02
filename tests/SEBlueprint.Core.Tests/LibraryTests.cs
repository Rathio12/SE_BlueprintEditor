using SEBlueprint.Core.Data;
using SEBlueprint.Core.Library;
using SEBlueprint.Core.Paths;

namespace SEBlueprint.Core.Tests;

public class LibraryTests
{
    [Fact]
    public async Task Enumerates_and_analyzes_with_errors_isolated()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        var good = Directory.CreateDirectory(Path.Combine(root, "Good")).FullName;
        File.Copy(TestData.Fx("bp_test", "bp.sbc"), Path.Combine(good, "bp.sbc"));
        File.WriteAllBytes(Path.Combine(good, "thumb.png"), new byte[] { 1, 2, 3 });
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(root, "Bad")).FullName, "bp.sbc"), "<x");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(root, "Bin")).FullName, "bp.sbcB5"), "bin");
        Directory.CreateDirectory(Path.Combine(root, "Empty"));
        var nested = Directory.CreateDirectory(Path.Combine(root, "Folder", "Nested")).FullName;
        File.Copy(TestData.Fx("bp_test", "bp.sbc"), Path.Combine(nested, "bp.sbc"));

        var e = BlueprintLibrary.Enumerate(new[] { new BlueprintRoot(root, "Local"), new BlueprintRoot(Path.Combine(root, "missing"), "Cloud") });
        Assert.Equal(4, e.Count);
        Assert.All(e, x => Assert.Equal("Local", x.Source));
        Assert.NotNull(e.Single(x => x.Name == "Good").ThumbPath);

        var (data, ws) = TestData.Fake();
        var progress = new List<int>();
        await BlueprintLibrary.AnalyzeAllAsync(e, GameDatabaseLoader.Load(data, ws), new SyncProgress(progress), default);
        Assert.NotNull(e.Single(x => x.Name == "Good").Report);
        Assert.NotNull(e.Single(x => x.Name == "Nested").Report);
        Assert.NotNull(e.Single(x => x.Name == "Bad").Error);
        Assert.Null(e.Single(x => x.Name == "Bad").Report);
        Assert.Contains("sbcB5", e.Single(x => x.Name == "Bin").Error);
        Assert.Equal(e.Count, progress.Max());
    }

    [Fact]
    public async Task Cancellation_stops_without_throwing()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        File.Copy(TestData.Fx("bp_test", "bp.sbc"), Path.Combine(Directory.CreateDirectory(Path.Combine(root, "A")).FullName, "bp.sbc"));
        var e = BlueprintLibrary.Enumerate(new[] { new BlueprintRoot(root, "Local") });
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await BlueprintLibrary.AnalyzeAllAsync(e, new GameDatabase(), null, cts.Token);
        Assert.Null(e[0].Report);
    }

    sealed class SyncProgress(List<int> sink) : IProgress<int>
    {
        public void Report(int value) { lock (sink) sink.Add(value); }
    }
}
