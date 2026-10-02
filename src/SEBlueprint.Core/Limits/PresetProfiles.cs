namespace SEBlueprint.Core.Limits;

public static class PresetProfiles
{
    public const string SigmaDraconisExpanse = "Sigma Draconis – Expanse";
    public const string ModdedServer = "Modded server (typical)";

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
