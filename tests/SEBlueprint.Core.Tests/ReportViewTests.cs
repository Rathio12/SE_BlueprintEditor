using System.Globalization;
using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Blueprints;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Presentation;

namespace SEBlueprint.Core.Tests;

public class ReportViewTests
{
    [Fact]
    public void Builds_readouts_checks_and_cost_like_the_app()
    {
        var old = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var (data, ws) = TestData.Fake();
            var db = GameDatabaseLoader.Load(data, ws);
            var report = BlueprintAnalyzer.Analyze(BlueprintData.Load(TestData.Fx("bp_test", "bp.sbc")), db);
            var view = ReportView.Build(report, new LimitProfile { Name = "S", TotalPcu = 500, MaxGuns = 10 }, db);

            Assert.Equal(new[] { "BLOCKS", "PCU", "GUNS", "CARGO" }, view.Primary.Select(p => p.Title));
            Assert.Equal("4", view.Primary.Single(p => p.Title == "GUNS").Value);
            Assert.Equal(Tone.Over, view.Primary.Single(p => p.Title == "PCU").Tone);
            Assert.Equal("/ 500", view.Primary.Single(p => p.Title == "PCU").LimitText);
            Assert.Equal(LimitStatus.Over, view.Overall);
            Assert.Equal("OVER LIMIT", view.OverallText);
            Assert.Equal("MODDED", view.Badge);
            Assert.Contains(view.Checks, c => c.Stat == "Guns" && c.StatusText == "OK");
            Assert.Equal("Steel Plate", view.Components.Single().Name);
            Assert.Contains("Ingot (kg)", view.CostAsText(','));
            Assert.Single(view.Unknown);
            Assert.Contains(view.MissingMods, m => m.Contains("42"));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void Partial_se2_report_only_shows_blocks_and_pcu()
    {
        var view = ReportView.Build(new BlueprintReport { IsPartial = true, Blocks = 10, Pcu = 20 }, new LimitProfile { MaxGuns = 1 }, new GameDatabase());
        Assert.Equal(new[] { "BLOCKS", "PCU" }, view.Primary.Select(p => p.Title));
        Assert.Empty(view.Secondary);
        Assert.Empty(view.Checks);
        Assert.Equal("SE2", view.Badge);
    }
}
