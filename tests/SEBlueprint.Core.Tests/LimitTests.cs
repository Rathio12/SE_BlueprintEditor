using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.Core.Tests;

public class LimitTests
{
    [Theory]
    [InlineData(50.0, null, LimitStatus.Unlimited)]
    [InlineData(50.0, 0.0, LimitStatus.Unlimited)]
    [InlineData(50.0, 100.0, LimitStatus.Ok)]
    [InlineData(90.0, 100.0, LimitStatus.Warn)]
    [InlineData(100.0, 100.0, LimitStatus.Warn)]
    [InlineData(101.0, 100.0, LimitStatus.Over)]
    public void Status(double v, double? l, LimitStatus s) => Assert.Equal(s, LimitEvaluator.StatusFor(v, l));

    [Fact]
    public void Evaluates_report_against_profile()
    {
        var r = new BlueprintReport { Pcu = 1200, Blocks = 10, FixedWeapons = 1, Turrets = 3, CargoLiters = 10 };
        r.Grids.Add(new GridSummary { Name = "A", Blocks = 10 });
        r.BlockPairCounts["Assembler"] = 3;
        var p = new LimitProfile { TotalPcu = 1000, MaxTurrets = 3, MaxBlocksPerGrid = 100, BlockTypeLimits = { ["Assembler"] = 2, ["Refinery"] = 1 } };
        var c = LimitEvaluator.Evaluate(r, p);
        Assert.Equal(LimitStatus.Over, c.Single(x => x.Stat == "PCU").Status);
        Assert.Equal(LimitStatus.Warn, c.Single(x => x.Stat == "Turrets").Status);
        Assert.Equal(LimitStatus.Ok, c.Single(x => x.Stat == "Largest grid").Status);
        Assert.Equal(LimitStatus.Unlimited, c.Single(x => x.Stat == "Guns").Status);
        Assert.Equal(LimitStatus.Over, c.Single(x => x.Stat == "Type: Assembler").Status);
        Assert.Equal(LimitStatus.Ok, c.Single(x => x.Stat == "Type: Refinery").Status);
        Assert.Equal(LimitStatus.Over, LimitEvaluator.Worst(c));
        Assert.Equal(1.2, c.Single(x => x.Stat == "PCU").Ratio, 6);
    }

    [Fact]
    public void Worst_of_nothing_limited_is_unlimited()
    {
        var c = LimitEvaluator.Evaluate(new BlueprintReport(), new LimitProfile());
        Assert.Equal(LimitStatus.Unlimited, LimitEvaluator.Worst(c));
    }

    [Fact]
    public void Vanilla_profiles_from_custom_worlds()
    {
        var ps = VanillaProfiles.Load(TestData.Fx("CustomWorlds"));
        Assert.Equal("Vanilla – No limits", ps[0].Name);
        Assert.True(ps.All(p => p.BuiltIn));
        var s = ps.Single(p => p.Name == "Vanilla – Star System");
        Assert.Equal(100000, s.TotalPcu);
        Assert.Equal(50000, s.MaxBlocksPerGrid);
        Assert.Equal(100000, s.MaxBlocksTotal);
        Assert.Equal(24, s.BlockTypeLimits["Assembler"]);
        Assert.DoesNotContain(ps, p => p.Name == "Vanilla – Empty World"); // identical to "No limits"
        Assert.Single(VanillaProfiles.Load(null));
    }

    [Fact]
    public void Profile_store_roundtrip()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        ProfileStore.Save(new LimitProfile { Name = "My Server: PvP/1", MaxGuns = 20 }, dir);
        var loaded = Assert.Single(ProfileStore.LoadUser(dir));
        Assert.Equal(20, loaded.MaxGuns);
        Assert.Equal("My Server: PvP/1", loaded.Name);
        Assert.False(loaded.BuiltIn);
        ProfileStore.Delete("My Server: PvP/1", dir);
        Assert.Empty(ProfileStore.LoadUser(dir));
    }

    [Fact]
    public void Profile_store_skips_broken_files_and_json_import_works()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        File.WriteAllText(Path.Combine(dir, "broken.json"), "{ nope");
        Assert.Empty(ProfileStore.LoadUser(dir));
        var p = ProfileStore.FromJson("{\"Name\":\"Imported\",\"TotalPcu\":5000}");
        Assert.Equal(5000, p.TotalPcu);
        Assert.Contains("Imported", ProfileStore.ToJson(p));
    }
}
