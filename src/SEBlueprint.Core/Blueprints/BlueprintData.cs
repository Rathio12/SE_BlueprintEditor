using System.Xml;
using System.Xml.Linq;
using SEBlueprint.Core.Data;

namespace SEBlueprint.Core.Blueprints;

public sealed record BlockRef(string Id, string Forward);

public sealed record GridData(string Name, string GridSize, IReadOnlyList<BlockRef> Blocks);

public sealed record BlueprintMod(string Id, string Name);

public sealed record BlueprintData(string Name, string DisplayName, string Owner, IReadOnlyList<GridData> Grids, IReadOnlyList<BlueprintMod> Mods)
{
    public static BlueprintData Load(string bpSbcPath)
    {
        using var s = File.OpenRead(bpSbcPath);
        return Parse(s, Path.GetFileName(Path.GetDirectoryName(bpSbcPath)) ?? "Blueprint");
    }

    public static BlueprintData Parse(Stream stream, string fallbackName)
    {
        XDocument doc;
        try
        {
            doc = Parsing.XmlFile.Load(stream);
        }
        catch (Exception ex) when (ex is XmlException or InvalidDataException)
        {
            throw new InvalidDataException($"Not a valid blueprint file: {ex.Message}", ex);
        }

        var bp = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "ShipBlueprint")
                 ?? throw new InvalidDataException("No ShipBlueprint element found");

        var name = (string?)Child(bp, "Id")?.Attribute("Subtype");
        var grids = bp.Descendants().Where(e => e.Name.LocalName == "CubeGrid").Select(ParseGrid).ToList();
        var mods = Child(bp, "Mods")?.Elements().Where(e => e.Name.LocalName == "ModItem").Select(ParseMod)
                       .Where(m => m.Id.Length > 0).ToList() ?? new List<BlueprintMod>();

        return new BlueprintData(
            string.IsNullOrWhiteSpace(name) ? fallbackName : name,
            Child(bp, "DisplayName")?.Value ?? "",
            Child(bp, "OwnerSteamId")?.Value ?? "",
            grids,
            mods);
    }

    static GridData ParseGrid(XElement grid)
    {
        var blocks = Child(grid, "CubeBlocks")?.Elements().Select(b =>
        {
            var type = DefinitionParser.StripPrefix(b.Attributes().FirstOrDefault(a => a.Name.LocalName == "type")?.Value ?? "CubeBlock");
            var sub = Child(b, "SubtypeName")?.Value ?? "";
            var forward = (string?)Child(b, "BlockOrientation")?.Attribute("Forward") ?? "Forward";
            return new BlockRef($"{type}/{sub}", forward);
        }).ToList() ?? new List<BlockRef>();
        return new GridData(Child(grid, "DisplayName")?.Value ?? "", Child(grid, "GridSizeEnum")?.Value == "Small" ? "Small" : "Large", blocks);
    }

    static BlueprintMod ParseMod(XElement mod)
    {
        var id = Child(mod, "PublishedFileId")?.Value;
        if (string.IsNullOrWhiteSpace(id) || id == "0")
            id = Path.GetFileNameWithoutExtension(Child(mod, "Name")?.Value ?? "");
        var name = (string?)mod.Attribute("FriendlyName");
        return new BlueprintMod(id.Trim(), string.IsNullOrWhiteSpace(name) ? id.Trim() : name);
    }

    static XElement? Child(XElement e, string name) => e.Elements().FirstOrDefault(c => c.Name.LocalName == name);
}
