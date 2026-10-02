using System.Text.Json;
using System.Text.Json.Serialization;

namespace SEBlueprint.Core.Data;

public sealed class GameDatabase
{
    public Dictionary<string, BlockDefinition> Blocks { get; set; } = new();

    public Dictionary<string, Dictionary<string, BlockDefinition>> ModBlocks { get; set; } = new();

    public Dictionary<string, ComponentDefinition> Items { get; set; } = new();

    public Dictionary<string, Dictionary<string, ComponentDefinition>> ModItems { get; set; } = new();

    public Dictionary<string, Dictionary<string, double>> Recipes { get; set; } = new();

    public Dictionary<string, Dictionary<string, Dictionary<string, double>>> ModRecipes { get; set; } = new();

    public Dictionary<string, string> ModNames { get; set; } = new();
    public string CacheKey { get; set; } = "";

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

    public Dictionary<string, double>? ResolveRecipe(string id, IReadOnlyList<string> enabledMods)
    {
        foreach (var mod in enabledMods)
            if (ModRecipes.TryGetValue(mod, out var map) && map.TryGetValue(id, out var r)) return r;
        return Recipes.GetValueOrDefault(id);
    }

    public ComponentDefinition? ResolveItem(string id, IReadOnlyList<string> enabledMods)
    {
        foreach (var mod in enabledMods)
            if (ModItems.TryGetValue(mod, out var map) && map.TryGetValue(id, out var i)) return i;
        if (Items.TryGetValue(id, out var vanilla)) return vanilla;
        foreach (var (_, map) in ModItems)
            if (map.TryGetValue(id, out var i)) return i;
        return null;
    }

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
