using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.Core.Tests;

public class GroupLimitTests
{
    static BlueprintReport Report(params Dictionary<string, int>[] grids)
    {
        var r = new BlueprintReport();
        var i = 0;
        foreach (var ids in grids)
        {
            var g = new GridSummary { Name = $"Grid {++i}", Blocks = ids.Values.Sum() };
            foreach (var (k, v) in ids) g.BlockIds[k] = v;
            r.Grids.Add(g);
            r.Blocks += g.Blocks;
        }
        return r;
    }

    static LimitCheck Check(BlueprintReport r, BlockGroupLimit g) =>
        LimitEvaluator.Evaluate(r, new LimitProfile { GroupLimits = { g } }).Single(c => c.Stat.StartsWith(g.Name));

    [Theory]
    [InlineData("Drill/LargeBlockDrill", "Drill", true)]
    [InlineData("Drill/LargeBlockDrill", "drill", true)]
    [InlineData("Drill/GoliathDrill2x", "Drill/Goliath*", true)]
    [InlineData("Reactor/LargeBlockLargeGeneratorWarfare216x", "Reactor/*16x", true)]
    [InlineData("Reactor/LargeBlockLargeGenerator8x", "Reactor/*16x", false)]
    [InlineData("Refinery/Blast Furnace", "Refinery/Blast*", true)]
    [InlineData("ShipWelder/LargeShipWelder", "ShipWelder/*Nanobot*", false)]
    [InlineData("OxygenGenerator/", "OxygenGenerator", true)]
    [InlineData("Drill/LargeBlockDrill", "Dril", false)]
    public void Patterns_match_type_and_subtype(string id, string pattern, bool expected) =>
        Assert.Equal(expected, BlockMatcher.Matches(id, new[] { pattern }));

    [Fact]
    public void Exclusions_remove_blocks_from_a_group()
    {
        string[] drills = { "Drill", "!Drill/Goliath*" };
        Assert.True(BlockMatcher.Matches("Drill/LargeBlockDrill8x", drills));
        Assert.False(BlockMatcher.Matches("Drill/GoliathDrill", drills));
        Assert.False(BlockMatcher.Matches("Drill/GoliathDrill", new[] { "!Drill/Goliath*" }));
    }

    [Fact]
    public void Per_grid_limits_use_the_grid_with_the_most_matching_blocks()
    {
        var r = Report(
            new Dictionary<string, int> { ["Reactor/LargeBlockLargeGenerator"] = 6, ["Reactor/LargeBlockLargeGenerator8x"] = 5, ["BatteryBlock/LargeBlockBatteryBlock"] = 3 },
            new Dictionary<string, int> { ["Reactor/SmallBlockSmallGenerator"] = 4 });
        var c = Check(r, new BlockGroupLimit { Name = "Reactors", Scope = LimitScope.Grid, Max = 10, Blocks = { "Reactor" } });
        Assert.Equal(11, c.Value);
        Assert.Equal(10, c.Limit);
        Assert.Equal(LimitStatus.Over, c.Status);
        Assert.Equal("Reactors per grid", c.Stat);
    }

    [Fact]
    public void Per_player_limits_count_every_grid_of_the_blueprint()
    {
        var r = Report(new Dictionary<string, int> { ["Drill/LargeBlockDrill"] = 6 }, new Dictionary<string, int> { ["Drill/SmallBlockDrill4x"] = 5, ["Drill/GoliathDrill"] = 3 });
        var c = Check(r, new BlockGroupLimit { Name = "Drills", Scope = LimitScope.Player, Max = 10, Blocks = { "Drill", "!Drill/Goliath*" } });
        Assert.Equal(11, c.Value);
        Assert.Equal(LimitStatus.Over, c.Status);
        Assert.Equal("Drills per player", c.Stat);
    }

    [Fact]
    public void A_limit_of_zero_means_the_block_is_not_allowed()
    {
        var rule = new BlockGroupLimit { Name = "Shield Air Pressurizer", Scope = LimitScope.Player, Max = 0, Blocks = { "OxygenGenerator/DSSupergen" } };
        Assert.Equal(LimitStatus.Over, Check(Report(new Dictionary<string, int> { ["OxygenGenerator/DSSupergen"] = 1 }), rule).Status);
        Assert.Equal(LimitStatus.Ok, Check(Report(new Dictionary<string, int> { ["OxygenGenerator/"] = 4 }), rule).Status);
    }

    [Fact]
    public void Near_the_limit_warns()
    {
        var c = Check(Report(new Dictionary<string, int> { ["BatteryBlock/LargeBlockBatteryBlock"] = 19 }),
            new BlockGroupLimit { Name = "Batteries", Max = 20, Blocks = { "BatteryBlock" } });
        Assert.Equal(LimitStatus.Warn, c.Status);
    }

    [Fact]
    public void Group_limits_survive_json_clone_and_change_the_limits_key()
    {
        var p = new LimitProfile { Name = "X", GroupLimits = { new BlockGroupLimit { Name = "Pistons", Scope = LimitScope.Player, Max = 5, Blocks = { "PistonBase", "ExtendedPistonBase" } } } };
        var json = ProfileStore.ToJson(p);
        Assert.Contains("\"Player\"", json);
        var back = ProfileStore.FromJson(json);
        var g = back.GroupLimits.Single();
        Assert.Equal(("Pistons", LimitScope.Player, 5), (g.Name, g.Scope, g.Max));
        Assert.Equal(new[] { "PistonBase", "ExtendedPistonBase" }, g.Blocks);
        var clone = p.Clone();
        clone.GroupLimits[0].Max = 6;
        Assert.Equal(5, p.GroupLimits[0].Max);
        Assert.NotEqual(new LimitProfile { Name = "X" }.LimitsKey(), p.LimitsKey());
    }

    [Fact]
    public void Stone_industries_profile_flags_what_is_over()
    {
        var si = PresetProfiles.All.Single(p => p.Name == PresetProfiles.StoneIndustries);
        var r = Report(new Dictionary<string, int>
        {
            ["Drill/LargeBlockDrill8x"] = 12,
            ["Drill/GoliathDrill"] = 1,
            ["Reactor/LargeBlockLargeGenerator16x"] = 4,
            ["Reactor/LargeBlockLargeGeneratorWarfare232x"] = 3,
            ["Assembler/FoodProcessor"] = 3,
            ["Assembler/LargeAssembler"] = 9,
            ["ShipWelder/SELtdLargeNanobotBuildAndRepairSystem"] = 1,
            ["ShipWelder/LargeShipWelder"] = 10,
        });
        var checks = LimitEvaluator.Evaluate(r, si).ToDictionary(c => c.Stat);
        Assert.Equal(LimitStatus.Over, checks["Drills per player"].Status);
        Assert.Equal(12, checks["Drills per player"].Value);
        Assert.Equal(1, checks["Goliath drills per player"].Value);
        Assert.Equal(LimitStatus.Over, checks["T4 + T5 reactors per grid"].Status);
        Assert.Equal(7, checks["Reactors per grid"].Value);
        Assert.Equal(9, checks["Assemblers per player"].Value);
        Assert.Equal(12, checks["Production blocks per grid"].Value);
        Assert.Equal(10, checks["Welders per player"].Value);
        Assert.Equal(1, checks["Build and Repair per grid"].Value);
        Assert.Equal(LimitStatus.Ok, checks["Shield Air Pressurizer per player"].Status);
        Assert.Equal(40000, si.MaxBlocksPerGrid);
    }
}
