using System.Xml.Linq;

namespace SEBlueprint.Core.Data;

/// <summary>
/// Builds a <see cref="GameDatabase"/> from individual files supplied as streams, so the same code works
/// for a local install (desktop app) and for files picked in a browser (website).
/// Every file is parsed in isolation: a broken file is recorded in <see cref="Errors"/> and skipped.
/// Mod content is kept per mod so one mod never changes the numbers of a blueprint that does not use it.
/// </summary>
public sealed class GameDatabaseBuilder
{
    readonly GameDatabase _db;
    readonly Dictionary<string, string> _texts = new();
    readonly Dictionary<string, Dictionary<string, bool>> _weaponCore = new();

    public List<string> Errors { get; } = new();

    public GameDatabaseBuilder(GameDatabase? baseDb = null)
    {
        _db = new GameDatabase();
        if (baseDb == null) return;
        _db.Blocks = new(baseDb.Blocks);
        _db.ModBlocks = baseDb.ModBlocks.ToDictionary(kv => kv.Key, kv => new Dictionary<string, BlockDefinition>(kv.Value));
        _db.Items = new(baseDb.Items);
        _db.ModItems = baseDb.ModItems.ToDictionary(kv => kv.Key, kv => new Dictionary<string, ComponentDefinition>(kv.Value));
        _db.Recipes = new(baseDb.Recipes);
        _db.ModRecipes = baseDb.ModRecipes.ToDictionary(kv => kv.Key, kv => new Dictionary<string, Dictionary<string, double>>(kv.Value));
        _db.ModNames = new(baseDb.ModNames);
    }

    /// <summary>Adds a vanilla file. <paramref name="relPath"/> is relative to Content/Data, e.g. "CubeBlocks/CubeBlocks_Armor.sbc".</summary>
    public void AddVanillaFile(string relPath, Func<Stream> open)
    {
        var rel = Normalize(relPath);
        var name = Path.GetFileName(rel);
        Guard(rel, () =>
        {
            if (name.Equals("MyTexts.resx", StringComparison.OrdinalIgnoreCase)) { ReadTexts(open); return; }
            if (!rel.EndsWith(".sbc", StringComparison.OrdinalIgnoreCase)) return;
            if (rel.StartsWith("CubeBlocks/", StringComparison.OrdinalIgnoreCase))
                AddBlocks(Load(open), null);
            else if (rel.StartsWith("Blueprints", StringComparison.OrdinalIgnoreCase))
                AddRecipes(Load(open), null);
            else if (name.StartsWith("Components", StringComparison.OrdinalIgnoreCase)
                     || name.StartsWith("PhysicalItems", StringComparison.OrdinalIgnoreCase)
                     || name.StartsWith("AmmoMagazines", StringComparison.OrdinalIgnoreCase))
                AddItems(Load(open), null);
        });
    }

    /// <summary>Adds a mod file. <paramref name="relPath"/> is relative to the mod folder, e.g. "Data/CubeBlocks.sbc".</summary>
    public void AddModFile(string modId, string relPath, Func<Stream> open)
    {
        var rel = Normalize(relPath);
        AddModName(modId, modId, overwrite: false);
        Guard($"{modId}/{rel}", () =>
        {
            if (rel.EndsWith(".sbc", StringComparison.OrdinalIgnoreCase))
            {
                var doc = Load(open);
                AddBlocks(doc, modId);
                AddItems(doc, modId);
                AddRecipes(doc, modId);
            }
            else if (rel.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                string text;
                using (var reader = new StreamReader(open())) text = reader.ReadToEnd();
                if (!text.Contains("MountPointDef", StringComparison.Ordinal)) return;
                var map = Sub(_weaponCore, modId);
                foreach (var (sub, turret) in WeaponCoreScanner.Scan(text)) map[sub] = map.GetValueOrDefault(sub) || turret;
            }
            else if (Path.GetFileName(rel).Equals("MyTexts.resx", StringComparison.OrdinalIgnoreCase))
                ReadTexts(open);
        });
    }

    public void AddModName(string modId, string name, bool overwrite = true)
    {
        if (overwrite || !_db.ModNames.ContainsKey(modId)) _db.ModNames[modId] = name;
    }

    /// <summary>Merges everything another builder collected (used to load mods in parallel, then combine in a fixed order).</summary>
    public void Merge(GameDatabaseBuilder other)
    {
        var o = other._db;
        foreach (var (k, v) in o.Blocks) _db.Blocks[k] = v;
        foreach (var (k, v) in o.Items) _db.Items[k] = v;
        foreach (var (k, v) in o.Recipes) _db.Recipes[k] = v;
        foreach (var (mod, map) in o.ModBlocks) foreach (var (k, v) in map) Sub(_db.ModBlocks, mod)[k] = v;
        foreach (var (mod, map) in o.ModItems) foreach (var (k, v) in map) Sub(_db.ModItems, mod)[k] = v;
        foreach (var (mod, map) in o.ModRecipes) foreach (var (k, v) in map) Sub(_db.ModRecipes, mod)[k] = v;
        foreach (var (k, v) in o.ModNames) _db.ModNames[k] = v;
        foreach (var (mod, map) in other._weaponCore) foreach (var (k, v) in map) Sub(_weaponCore, mod)[k] = v;
        foreach (var (k, v) in other._texts) _texts[k] = v;
        Errors.AddRange(other.Errors);
    }

    public GameDatabase Build()
    {
        foreach (var (modId, map) in _weaponCore)
        {
            if (!_db.ModBlocks.TryGetValue(modId, out var blocks)) continue;
            foreach (var b in blocks.Values)
                if (map.TryGetValue(b.SubtypeId, out var turret))
                    b.Category = turret ? BlockCategory.Turret : BlockCategory.FixedWeapon;
        }
        if (_texts.Count > 0)
        {
            foreach (var b in _db.Blocks.Values.Concat(_db.ModBlocks.Values.SelectMany(m => m.Values)))
                if (_texts.TryGetValue(b.DisplayName, out var t)) b.DisplayName = t;
            foreach (var i in _db.Items.Values.Concat(_db.ModItems.Values.SelectMany(m => m.Values)))
                if (_texts.TryGetValue(i.DisplayName, out var t)) i.DisplayName = t;
        }
        return _db;
    }

    void AddBlocks(XDocument doc, string? modId)
    {
        var target = modId == null ? _db.Blocks : Sub(_db.ModBlocks, modId);
        foreach (var b in DefinitionParser.ParseBlocks(doc, modId)) target[b.Id] = b;
    }

    void AddItems(XDocument doc, string? modId)
    {
        var target = modId == null ? _db.Items : Sub(_db.ModItems, modId);
        foreach (var i in DefinitionParser.ParseItems(doc)) target[i.Id] = i;
    }

    void AddRecipes(XDocument doc, string? modId)
    {
        var target = modId == null ? _db.Recipes : Sub(_db.ModRecipes, modId);
        foreach (var (result, inputs) in DefinitionParser.ParseRecipes(doc)) target[result] = inputs;
    }

    void ReadTexts(Func<Stream> open)
    {
        foreach (var data in Load(open).Descendants().Where(e => e.Name.LocalName == "data"))
        {
            var key = (string?)data.Attribute("name");
            var value = data.Elements().FirstOrDefault(e => e.Name.LocalName == "value")?.Value;
            if (!string.IsNullOrEmpty(key) && value != null) _texts[key] = value;
        }
    }

    void Guard(string what, Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            Errors.Add($"{what}: {ex.Message}");
            Log.Write($"Skipped {what}: {ex.Message}");
        }
    }

    static Dictionary<string, T> Sub<T>(Dictionary<string, Dictionary<string, T>> d, string key)
    {
        if (!d.TryGetValue(key, out var map)) d[key] = map = new();
        return map;
    }

    static XDocument Load(Func<Stream> open)
    {
        using var s = open();
        return Parsing.XmlFile.Load(s);
    }

    static string Normalize(string p) => p.Replace('\\', '/').TrimStart('/');
}
