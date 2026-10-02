using System.Xml.Linq;
using SEBlueprint.Core.Data;

namespace SEBlueprint.Core.Tests;

public class DefinitionParserTests
{
    static string FxPath(string n) => Path.Combine(AppContext.BaseDirectory, "Fixtures", n);
    static XDocument Fx(string n) => XDocument.Load(FxPath(n));

    [Fact]
    public void Parses_blocks_and_categories()
    {
        var b = DefinitionParser.ParseBlocks(Fx("CubeBlocks_Test.sbc"), null).ToDictionary(x => x.Id);
        Assert.False(b.ContainsKey("CubeBlock/NoComponents"));
        Assert.Equal(BlockCategory.Turret, b["LargeGatlingTurret/"].Category);
        Assert.Equal(15, b["LargeGatlingTurret/"].Components["Component/SteelPlate"]);
        Assert.Equal("Gatling Turret", b["LargeGatlingTurret/"].DisplayName);
        Assert.Equal(BlockCategory.FixedWeapon, b["SmallGatlingGun/SmallGatling"].Category);
        Assert.Equal("Small", b["SmallGatlingGun/SmallGatling"].GridSize);
        Assert.Equal(2, b["SmallGatlingGun/SmallGatling"].SizeZ);
        Assert.Equal(BlockCategory.Cargo, b["CargoContainer/LargeBlockLargeContainer"].Category);
        Assert.Equal(8000, b["CargoContainer/LargeBlockLargeContainer"].CargoLiters, 3);
        Assert.Equal("LargeContainer", b["CargoContainer/LargeBlockLargeContainer"].PairName);
        Assert.Equal(0.5 * 0.5 * 0.5 * 1000 * 0.4, b["CargoContainer/SmallNoInv"].CargoLiters, 3);
        Assert.Equal(BlockCategory.Thruster, b["Thrust/LargeBlockLargeThrust"].Category);
        Assert.Equal(4320000, b["Thrust/LargeBlockLargeThrust"].ThrustForce);
        Assert.Equal(0.3, b["Thrust/LargeBlockLargeThrust"].ThrustPlanetEff, 3);
        Assert.Equal(BlockCategory.Power, b["BatteryBlock/LargeBlockBatteryBlock"].Category);
        Assert.Equal(12, b["BatteryBlock/LargeBlockBatteryBlock"].PowerOutput);
        Assert.Equal(3, b["BatteryBlock/LargeBlockBatteryBlock"].PowerStorage);
        Assert.False(b["BatteryBlock/LargeBlockBatteryBlock"].IsGenerator);
        Assert.Equal(BlockCategory.JumpDrive, b["JumpDrive/LargeJumpDrive"].Category);
        Assert.Equal(2000, b["JumpDrive/LargeJumpDrive"].JumpDistance);
        Assert.Equal(1250000, b["JumpDrive/LargeJumpDrive"].JumpMaxMass);
        Assert.Equal(BlockCategory.Other, b["ConveyorSorter/M12Swarm"].Category);
    }

    [Fact]
    public void Parses_recipes_per_unit_and_skips_scrap_and_multi_output()
    {
        var r = DefinitionParser.ParseRecipes(Fx("Blueprints_Test.sbc")).ToList();
        Assert.Contains(r, x => x.Result == "Component/SteelPlate" && x.InputsPerUnit["Ingot/Iron"] == 21);
        var iron = Assert.Single(r, x => x.Result == "Ingot/Iron");
        Assert.Equal(1 / 0.7, iron.InputsPerUnit["Ore/Iron"], 6);
        Assert.DoesNotContain(r, x => x.Result == "Ingot/Stone");
    }

    [Fact]
    public void Parses_items_with_mass()
    {
        var items = DefinitionParser.ParseItems(Fx("Components_Test.sbc")).ToDictionary(c => c.Id);
        Assert.Equal(20, items["Component/SteelPlate"].Mass);
        Assert.Equal(1, items["Ingot/Iron"].Mass);
        Assert.Equal(35, items["AmmoMagazine/NATO_25x184mm"].Mass);
    }

    [Fact]
    public void WeaponCore_scan_detects_fixed_and_turret()
    {
        var s = WeaponCoreScanner.Scan(File.ReadAllText(FxPath("WeaponCorePart.cs.txt")));
        Assert.False(s["M12Swarm"]);
        Assert.True(s["BigTurret"]);
    }

    [Fact]
    public void Malformed_definition_elements_do_not_throw()
    {
        var doc = XDocument.Parse("<Definitions><CubeBlocks><Definition><Id><TypeId>X</TypeId></Id><Components><Component Subtype=\"A\" Count=\"oops\"/></Components><Size x=\"q\"/></Definition></CubeBlocks></Definitions>");
        var b = Assert.Single(DefinitionParser.ParseBlocks(doc, "m1"));
        Assert.Equal("X/", b.Id);
        Assert.Equal(0, b.Components["Component/A"]);
        Assert.Equal("m1", b.ModId);
        Assert.Equal(1, b.SizeX);
    }
}
