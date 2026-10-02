namespace SEBlueprint.Core.Limits;

public static class PresetProfiles
{
    public const string SigmaDraconisExpanse = "Sigma Draconis – Expanse";
    public const string ModdedServer = "Modded server (typical)";
    public const string StoneIndustries = "Stone Industries (SI)";

    public static IReadOnlyList<LimitProfile> All { get; } = new[]
    {
        new LimitProfile
        {
            Name = SigmaDraconisExpanse,
            BuiltIn = true,
            Description = "Published limit of the Sigma Draconis Expanse server: 50,000 PCU per grid (sigmadraconis.games, checked October 2026). Other server rules are not published as numbers; check the server wiki before a fight.",
            MaxPcuPerGrid = 50000,
        },
        new LimitProfile
        {
            Name = StoneIndustries,
            BuiltIn = true,
            Description = "Stone Industries Gaming limits from the #block-limits channel (May 2025): grids are capped at 40,000 blocks, no PCU limit, " +
                          "and the per-player and per-grid block rules below (any combination of type or tier). Per-player rules are checked against this blueprint alone; " +
                          "your other grids on the server count too. T4 + T5 reactors are taken as Prosonic (16x) and Tellurium (32x). " +
                          "Weapons are usually deleted when over a limit, most other blocks are shut down.",
            MaxBlocksPerGrid = 40000,
            GroupLimits =
            {
                Player("Refineries", 10, "Refinery"),
                Player("Assemblers", 10, "Assembler", "!Assembler/FoodProcessor"),
                Player("Drills", 10, "Drill", "!Drill/Goliath*"),
                Player("Grinders", 10, "ShipGrinder"),
                Player("Welders", 10, "ShipWelder", "!ShipWelder/*Nanobot*"),
                Player("Build and Repair", 2, "ShipWelder/*Nanobot*"),
                Player("Goliath drills", 2, "Drill/Goliath*"),
                Player("Shield Air Pressurizer", 0, "OxygenGenerator/DSSupergen"),
                Player("Pistons", 5, "PistonBase", "ExtendedPistonBase"),
                Player("Rotors and hinges", 10, "MotorStator", "MotorAdvancedStator"),
                Player("Remote controls", 5, "RemoteControl"),
                Grid("Production blocks", 30, "Refinery", "Assembler", "OxygenGenerator", "OxygenFarm", "!OxygenGenerator/DSSupergen"),
                Grid("Reactors", 10, "Reactor"),
                Grid("T4 + T5 reactors", 6, "Reactor/*16x", "Reactor/*32x"),
                Grid("Batteries", 20, "BatteryBlock"),
                Grid("Solar panels", 50, "SolarPanel"),
                Grid("Wind turbines", 50, "WindTurbine"),
                Grid("Hydrogen engines", 10, "HydrogenEngine"),
                Grid("Build and Repair", 1, "ShipWelder/*Nanobot*"),
                Grid("Goliath drills", 2, "Drill/Goliath*"),
                Grid("Survival kits", 2, "SurvivalKit"),
                Grid("Gravity generators", 6, "GravityGenerator", "GravityGeneratorSphere"),
            },
        },
        new LimitProfile
        {
            Name = ModdedServer,
            BuiltIn = true,
            Description = "A typical starting point for modded survival servers. Not tied to any specific server; duplicate it and adjust to your server's rules.",
            TotalPcu = 100000,
            MaxPcuPerGrid = 50000,
            MaxBlocksPerGrid = 25000,
            MaxGuns = 40,
            MaxTurrets = 30,
            BlockTypeLimits = new Dictionary<string, int>
            {
                ["Assembler"] = 6,
                ["Refinery"] = 6,
                ["Drill"] = 30,
                ["ShipGrinder"] = 20,
                ["JumpDrive"] = 8,
                ["Projector"] = 4,
                ["ProgrammableBlock"] = 10,
            },
        },
    };

    static BlockGroupLimit Player(string name, int max, params string[] blocks) =>
        new() { Name = name, Scope = LimitScope.Player, Max = max, Blocks = blocks.ToList() };

    static BlockGroupLimit Grid(string name, int max, params string[] blocks) =>
        new() { Name = name, Scope = LimitScope.Grid, Max = max, Blocks = blocks.ToList() };
}
