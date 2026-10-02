using System.Text.Json;
using System.Text.Json.Serialization;

namespace SEBlueprint.Core.Data;

/// <summary>All block, item and recipe definitions known for one game install plus its Workshop mods.</summary>
public sealed class GameDatabase
{
    /// <summary>Vanilla blocks by id.</summary>
    public Dictionary<string, BlockDefinition> Blocks { get; set; } = new();
    /// <summary>Mod id → block id → definition.</summary>
    public Dictionary<string, Dictionary<string, BlockDefinition>> ModBlocks { get; set; } = new();
    /// <summary>Vanilla components, ingots, ores and ammo.</summary>
    public Dictionary<string, ComponentDefinition> Items { get; set; } = new();
    /// <summary>Mod id → item id → definition. Only applied when a blueprint uses that mod.</summary>
    public Dictionary<string, Dictionary<string, ComponentDefinition>> ModItems { get; set; } = new();
    /// <summary>Vanilla recipes: result item id → inputs per one unit.</summary>
    public Dictionary<string, Dictionary<string, double>> Recipes { get; set; } = new();
    /// <summary>Mod id → result item id → inputs per one unit. Only applied when a blueprint uses that mod.</summary>
    public Dictionary<string, Dictionary<string, Dictionary<string, double>>> ModRecipes { get; set; } = new();
    /// <summary>Every mod folder found on disk (id → display name).</summary>
    public Dictionary<string, string> ModNames { get; set; } = new();
    public string CacheKey { get; set; } = "";

    /// <summary>
    /// Finds a block definition: mods the blueprint lists (in order) → vanilla → any other mod on disk.
    /// <paramref name="fromUnlistedMod"/> is true when only a mod the blueprint did not list defines it.
    /// </summary>
    public BlockDefinition? Resolve(string id, IReadOnlyList<string> enabledMods, out bool fromUnlistedMod)
    {
        fromUnlistedMod = false;
        foreach (var mod in enabledMods)
            if (ModBlocks.TryGetValue(mod, out var blocks) && blocks.TryGetValue(id, out var def))
                return def;
        if (Blocks.TryGetValue(id, out var vanilla))
            return vanilla;
        foreach (var (_, blocks) in ModBlocks)
            if (blocks.TryGetValue(id, out var def))
            {
                fromUnlistedMod = true;
                return def;
            }
        return null;
    }

    /// <summary>Recipe for an item: enabled mods first (in order), then vanilla.</summary>
    public Dictionary<string, double>? ResolveRecipe(string id, IReadOnlyList<string> enabledMods)
    {
        foreach (var mod in enabledMods)
            if (ModRecipes.TryGetValue(mod, out var map) && map.TryGetValue(id, out var r)) return r;
        return Recipes.GetValueOrDefault(id);
    }

    /// <summary>Item definition: enabled mods first (in order), then vanilla.</summary>
    public ComponentDefinition? ResolveItem(string id, IReadOnlyList<string> enabledMods)
    {
        foreach (var mod in enabledMods)
            if (ModItems.TryGetValue(mod, out var map) && map.TryGetValue(id, out var i)) return i;
        if (Items.TryGetValue(id, out var vanilla)) return vanilla;
        foreach (var (_, map) in ModItems)
            if (map.TryGetValue(id, out var i)) return i;
        return null;
    }

    /// <summary>Human readable item name, falling back to the subtype id.</summary>
    public string ItemName(string id)
    {
        var name = ResolveItem(id, Array.Empty<string>())?.DisplayName;
        return !string.IsNullOrWhiteSpace(name) && !name.StartsWith("DisplayName_", StringComparison.Ordinal)
            ? name
            : id[(id.IndexOf('/') + 1)..];
    }

    static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static GameDatabase FromJson(string json) =>
        JsonSerializer.Deserialize<GameDatabase>(json, Json) ?? throw new JsonException("Empty game database");
}
