using System.Xml.Linq;
using SEBlueprint.Core.Parsing;

namespace SEBlueprint.Core.Data;

public static class DefinitionParser
{
    public const double LargeCube = 2.5, SmallCube = 0.5;
    const string Prefix = "MyObjectBuilder_";

    static readonly HashSet<string> TurretTypes = new() { "LargeGatlingTurret", "LargeMissileTurret", "InteriorTurret" };
    static readonly HashSet<string> FixedWeaponTypes = new() { "SmallGatlingGun", "SmallMissileLauncher", "SmallMissileLauncherReload" };
    static readonly HashSet<string> PowerTypes = new() { "Reactor", "BatteryBlock", "SolarPanel", "WindTurbine", "HydrogenEngine" };
    static readonly HashSet<string> PowerGroups = new() { "SolarPanels", "Battery", "Reactors" };

    public static string StripPrefix(string? typeId) =>
        typeId is null ? "" : typeId.StartsWith(Prefix, StringComparison.Ordinal) ? typeId[Prefix.Length..] : typeId;

    public static IEnumerable<BlockDefinition> ParseBlocks(XDocument doc, string? modId)
    {
        foreach (var def in doc.Descendants().Where(e => e.Name.LocalName == "Definition"))
        {
            var id = ReadId(def);
            var components = Child(def, "Components");
            if (id == null || components == null) continue;

            var b = new BlockDefinition { Id = id, ModId = modId };
            foreach (var c in components.Elements().Where(e => e.Name.LocalName == "Component"))
            {
                var key = "Component/" + (string?)c.Attribute("Subtype");
                b.Components[key] = b.Components.GetValueOrDefault(key) + Num.I((string?)c.Attribute("Count"));
            }
            b.DisplayName = Text(def, "DisplayName") ?? "";
            b.Icon = IconPath(def);
            b.PairName = Text(def, "BlockPairName");
            b.GridSize = Text(def, "CubeSize") == "Small" ? "Small" : "Large";
            b.Pcu = Num.I(Text(def, "PCU"));
            ReadSize(Child(def, "Size"), b);
            ReadStats(def, b);
            b.Category = Categorize(b.TypeId, def, b);
            yield return b;
        }
    }

    public static IEnumerable<ComponentDefinition> ParseItems(XDocument doc)
    {
        foreach (var e in doc.Descendants().Where(e => e.Name.LocalName is "Component" or "PhysicalItem" or "AmmoMagazine"))
        {
            var id = ReadId(e);
            if (id == null) continue;
            yield return new ComponentDefinition
            {
                Id = id,
                DisplayName = Text(e, "DisplayName") ?? "",
                Icon = IconPath(e),
                Mass = Num.D(Text(e, "Mass")),
                Volume = Num.D(Text(e, "Volume")),
            };
        }
    }

    public static IEnumerable<(string Result, Dictionary<string, double> InputsPerUnit)> ParseRecipes(XDocument doc)
    {
        foreach (var bp in doc.Descendants().Where(e => e.Name.LocalName == "Blueprint"))
        {
            var inputs = Items(Child(bp, "Prerequisites"));
            var results = Items(Child(bp, "Results"));
            var single = Child(bp, "Result");
            if (single != null) results.Add(ItemKey(single), Num.D((string?)single.Attribute("Amount")));

            if (inputs.Count == 0 || results.Count != 1) continue;
            var (result, amount) = results.First();
            if (amount <= 0 || inputs.ContainsKey(result) || inputs.ContainsKey("Ore/Ice")
                || inputs.Keys.Any(k => k.EndsWith("/Scrap", StringComparison.Ordinal))) continue;

            yield return (result, inputs.ToDictionary(kv => kv.Key, kv => kv.Value / amount));
        }
    }

    public static BlockCategory Categorize(string typeId, XElement def) => Categorize(typeId, def, null);

    static BlockCategory Categorize(string typeId, XElement def, BlockDefinition? b)
    {
        if (TurretTypes.Contains(typeId) || typeId.EndsWith("Turret", StringComparison.Ordinal)) return BlockCategory.Turret;
        if (FixedWeaponTypes.Contains(typeId) || Child(def, "WeaponDefinitionId") != null) return BlockCategory.FixedWeapon;
        if (typeId == "CargoContainer") return BlockCategory.Cargo;
        if (typeId == "Thrust" || b?.ThrustForce > 0) return BlockCategory.Thruster;
        if (typeId == "JumpDrive") return BlockCategory.JumpDrive;
        if (b != null && (b.PowerOutput > 0 || b.PowerStorage > 0)) return BlockCategory.Power;
        return BlockCategory.Other;
    }

    static void ReadStats(XElement def, BlockDefinition b)
    {
        var group = Text(def, "ResourceSourceGroup") ?? Text(def, "ResourceSinkGroup");
        if (b.TypeId == "Thrust" || group == "Thrust")
        {
            b.ThrustForce = Num.D(Text(def, "ForceMagnitude"));
            b.ThrustSpaceEff = Num.D(Text(def, "EffectivenessAtMinInfluence"), 1);
            b.ThrustPlanetEff = Num.D(Text(def, "EffectivenessAtMaxInfluence"), 1);
        }
        if (PowerTypes.Contains(b.TypeId) || (group != null && PowerGroups.Contains(group)))
        {
            b.PowerOutput = Num.D(Text(def, "MaxPowerOutput"));
            b.PowerStorage = Num.D(Text(def, "MaxStoredPower"));
            b.IsGenerator = Child(def, "RequiredPowerInput") == null;
        }
        if (b.TypeId == "JumpDrive")
        {
            b.JumpDistance = Num.D(Text(def, "MaxJumpDistance")) / 1000.0;
            b.JumpMaxMass = Num.D(Text(def, "MaxJumpMass"));
        }
        var inv = Child(def, "InventorySize");
        if (inv != null)
            b.CargoLiters = Num.D(Text(inv, "X")) * Num.D(Text(inv, "Y")) * Num.D(Text(inv, "Z")) * 1000.0;
        else if (b.TypeId == "CargoContainer")
        {
            var cube = b.GridSize == "Small" ? SmallCube : LargeCube;
            b.CargoLiters = b.SizeX * b.SizeY * b.SizeZ * cube * cube * cube * 1000.0;
        }
    }

    static void ReadSize(XElement? size, BlockDefinition b)
    {
        if (size == null) return;
        int Axis(string attr, string child)
        {
            var v = Num.I((string?)size.Attribute(attr) ?? Text(size, child), 1);
            return v > 0 ? v : 1;
        }
        b.SizeX = Axis("x", "X");
        b.SizeY = Axis("y", "Y");
        b.SizeZ = Axis("z", "Z");
    }

    static Dictionary<string, double> Items(XElement? list)
    {
        var d = new Dictionary<string, double>();
        if (list == null) return d;
        foreach (var i in list.Elements().Where(e => e.Name.LocalName == "Item"))
        {
            var k = ItemKey(i);
            d[k] = d.GetValueOrDefault(k) + Num.D((string?)i.Attribute("Amount"));
        }
        return d;
    }

    static string ItemKey(XElement e) => StripPrefix((string?)e.Attribute("TypeId")) + "/" + (string?)e.Attribute("SubtypeId");

    static string? ReadId(XElement e)
    {
        var id = Child(e, "Id");
        if (id == null) return null;
        var type = Text(id, "TypeId") ?? (string?)id.Attribute("Type");
        if (string.IsNullOrWhiteSpace(type)) return null;
        var sub = Text(id, "SubtypeId") ?? (string?)id.Attribute("Subtype") ?? "";
        return StripPrefix(type.Trim()) + "/" + sub.Trim();
    }

    static string? IconPath(XElement e)
    {
        var icon = Text(e, "Icon")?.Trim();
        return string.IsNullOrEmpty(icon) ? null : icon.Replace(@"\", "/");
    }

    static XElement? Child(XElement e, string name) => e.Elements().FirstOrDefault(c => c.Name.LocalName == name);
    static string? Text(XElement e, string name) => Child(e, name)?.Value;
}
