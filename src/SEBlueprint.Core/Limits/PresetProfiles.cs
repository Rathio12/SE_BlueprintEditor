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
            Description = "Stone Industries Gaming limits from the #block-limits channel (May 2025): grids are capped at 40,000 blocks, no PCU limit. " +
                          "Also enforced by the server but not checked here (any combination of type or tier). " +
                          "Per player: refineries 10, assemblers 10 (food processors don't count), drills 10, grinders 10, welders 10, Build and Repair 2, Goliath drills 2, Shield Air Pressurizer 0, pistons 5, rotors/hinges 10, remote controls 5. " +
                          "Per grid: production blocks 30 (refineries, assemblers, O2 generators/farms, food processors, irrigation), reactors 10 (T4 + T5 max 6), batteries 20, solar panels 50, wind turbines 50, hydrogen engines 10, Build and Repair 1, Goliath drills 2, survival kits 2, gravity generators 6. " +
                          "Weapons are usually deleted when over a limit, most other blocks are shut down.",
            MaxBlocksPerGrid = 40000,
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
}
