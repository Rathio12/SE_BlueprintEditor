using System.Xml.Linq;
using SEBlueprint.Core.Parsing;

namespace SEBlueprint.Core.Limits;

/// <summary>Builds limit profiles from the world presets that ship with the game (Content/CustomWorlds/*/Sandbox_config.sbc).</summary>
public static class VanillaProfiles
{
    public const string NoLimitsName = "Vanilla – No limits";

    public static IReadOnlyList<LimitProfile> Load(string? customWorldsDir)
    {
        var configs = new List<LimitProfile>();
        try
        {
            if (!string.IsNullOrWhiteSpace(customWorldsDir) && Directory.Exists(customWorldsDir))
            {
                foreach (var world in Directory.EnumerateDirectories(customWorldsDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                {
                    var file = Directory.EnumerateFiles(world, "Sandbox_config.sbc", SearchOption.AllDirectories)
                        .OrderBy(f => f.Length).FirstOrDefault();
                    if (file == null) continue;
                    try
                    {
                        using var s = File.OpenRead(file);
                        configs.Add(ParseConfig(Path.GetFileName(world), s));
                    }
                    catch (Exception ex) { Log.Write($"Skipped world config {file}: {ex.Message}"); }
                }
            }
        }
        catch (Exception ex) { Log.Write($"Cannot read {customWorldsDir}: {ex.Message}"); }
        return WithNoLimits(configs);
    }

    /// <summary>Prepends the "No limits" profile and drops presets whose limits are identical to an earlier one.</summary>
    public static IReadOnlyList<LimitProfile> WithNoLimits(IEnumerable<LimitProfile> configs)
    {
        var result = new List<LimitProfile> { new() { Name = NoLimitsName, BuiltIn = true } };
        var seen = new HashSet<string> { result[0].LimitsKey() };
        foreach (var p in configs)
            if (seen.Add(p.LimitsKey())) result.Add(p);
        return result;
    }

    public static LimitProfile ParseConfig(string worldName, Stream stream)
    {
        var settings = XmlFile.Load(stream).Descendants().FirstOrDefault(e => e.Name.LocalName == "Settings")
                       ?? throw new InvalidDataException("No Settings element");
        int? Int(string name)
        {
            var v = Num.I(settings.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value);
            return v > 0 ? v : null;
        }
        var p = new LimitProfile
        {
            Name = $"Vanilla – {worldName}",
            BuiltIn = true,
            TotalPcu = Int("TotalPCU"),
            MaxBlocksPerGrid = Int("MaxGridSize"),
            MaxBlocksTotal = Int("MaxBlocksPerPlayer"),
        };
        var items = settings.Elements().FirstOrDefault(e => e.Name.LocalName == "BlockTypeLimits")?.Descendants().Where(e => e.Name.LocalName == "item");
        foreach (var item in items ?? Enumerable.Empty<XElement>())
        {
            var key = item.Elements().FirstOrDefault(e => e.Name.LocalName == "Key")?.Value;
            var value = Num.I(item.Elements().FirstOrDefault(e => e.Name.LocalName == "Value")?.Value);
            if (!string.IsNullOrWhiteSpace(key) && value > 0) p.BlockTypeLimits[key] = value;
        }
        return p;
    }
}
