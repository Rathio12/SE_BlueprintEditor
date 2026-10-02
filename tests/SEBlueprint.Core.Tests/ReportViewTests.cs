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
            Assert.Equal("1 UNKNOWN BLOCK — COUNTS INCOMPLETE", view.IncompleteText);
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
        Assert.Null(view.IncompleteText);
    }

    [Fact]
    public void Incomplete_blueprint_is_never_reported_as_within_limits()
    {
        var r = new BlueprintReport { Blocks = 30000, Pcu = 10 };
        r.Grids.Add(new GridSummary { Blocks = 30000, Pcu = 10 });
        r.UnknownBlocks["Mod/Block"] = 29990;
        var view = ReportView.Build(r, new LimitProfile { TotalPcu = 100000 }, new GameDatabase());
        Assert.NotEqual("WITHIN LIMITS", view.OverallText);
        Assert.Equal(Tone.Warn, view.OverallTone);
        Assert.StartsWith("29.990", view.IncompleteText!.Replace(",", "."));
    }

    [Fact]
    public void Per_grid_limits_drive_the_pcu_and_blocks_gauges_when_no_total_limit()
    {
        var r = new BlueprintReport { Blocks = 300, Pcu = 60000 };
        r.Grids.Add(new GridSummary { Blocks = 200, Pcu = 45000 });
        r.Grids.Add(new GridSummary { Blocks = 100, Pcu = 15000 });
        var view = ReportView.Build(r, new LimitProfile { MaxPcuPerGrid = 50000, MaxBlocksPerGrid = 1000 }, new GameDatabase());
        var pcu = view.Primary.Single(p => p.Title == "PCU");
        Assert.True(pcu.HasLimit);
        Assert.Equal(0.9, pcu.Ratio, 6);
        Assert.Equal(Tone.Warn, pcu.Tone);
        Assert.True(view.Primary.Single(p => p.Title == "BLOCKS").HasLimit);
    }

    [Fact]
    public void Group_rules_that_are_over_come_first_and_empty_rules_are_hidden()
    {
        var r = new BlueprintReport { Blocks = 30 };
        var g = new GridSummary { Blocks = 30 };
        g.BlockIds["Drill/LargeBlockDrill"] = 12;
        g.BlockIds["BatteryBlock/LargeBlockBatteryBlock"] = 5;
        g.BlockIds["CubeBlock/LargeBlockArmorBlock"] = 13;
        r.Grids.Add(g);
        var profile = new LimitProfile
        {
            MaxBlocksPerGrid = 1000,
            GroupLimits =
            {
                new BlockGroupLimit { Name = "Batteries", Max = 20, Blocks = { "BatteryBlock" } },
                new BlockGroupLimit { Name = "Pistons", Scope = LimitScope.Player, Max = 5, Blocks = { "PistonBase" } },
                new BlockGroupLimit { Name = "Drills", Scope = LimitScope.Player, Max = 10, Blocks = { "Drill" } },
            },
        };
        var view = ReportView.Build(r, profile, new GameDatabase());
        var stats = view.Checks.Select(c => c.Stat).ToList();
        Assert.Equal("Drills per player", stats.First());
        Assert.Contains("Batteries per grid", stats);
        Assert.DoesNotContain("Pistons per player", stats);
        Assert.Contains("Largest grid", stats);
        Assert.Equal("OVER LIMIT", view.OverallText);
    }
}
