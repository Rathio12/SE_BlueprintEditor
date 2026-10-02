using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SEBlueprint.Core.Analysis;
using SEBlueprint.Core.Blueprints;
using SEBlueprint.Core.Data;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.Web.Services;

public sealed class WebEntry
{
    public string Name { get; set; } = "";
    public string Source { get; init; } = "Upload";
    public BlueprintData? Data { get; init; }
    public BlueprintReport? Report { get; set; }
    public string? Error { get; set; }
    public string? Thumb { get; init; }
    public DateTime Added { get; init; } = DateTime.Now;
}

public sealed class WebState
{
    public const string Repo = "https://github.com/Rathio12/SE_BlueprintEditor";
    public const string ReportBug = Repo + "/issues/new/choose";
    public const string Releases = Repo + "/releases";
    const long MaxFileBytes = 300L * 1024 * 1024;
    const string ProfilesKey = "se.profiles";
    const string SettingsKey = "se.settings";

    readonly IJSRuntime _js;
    GameDatabase _vanilla = new();
    bool _loaded;

    public WebState(IJSRuntime js) => _js = js;

    public event Action? Changed;

    public GameDatabase Db { get; private set; } = new();
    public List<LimitProfile> Profiles { get; } = new();
    public LimitProfile Active { get; private set; } = new() { Name = VanillaProfiles.NoLimitsName, BuiltIn = true };
    public List<WebEntry> Entries { get; } = new();
    public WebEntry? Selected { get; set; }
    public double AssemblerEfficiency { get; private set; } = 1;
    public int YieldModules { get; private set; }
    public bool RatePromptDone { get; private set; }
    public List<string> LoadedMods { get; } = new();
    public string Status { get; private set; } = "Loading game data…";
    public bool Busy { get; private set; }
    public string? Error { get; set; }

    public double YieldMultiplier => YieldModules switch { 1 => 1.19, 2 => 1.41, 3 => 1.68, >= 4 => 2.0, _ => 1.0 };

    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            _vanilla = GameDatabase.FromJson(ReadResource("vanilla-db.json"));
            Db = _vanilla;
            var vanilla = JsonSerializer.Deserialize<List<LimitProfile>>(ReadResource("vanilla-profiles.json")) ?? new();
            Profiles.AddRange(vanilla);
            Profiles.AddRange(PresetProfiles.All);
            var stored = await _js.InvokeAsync<string?>("se.load", ProfilesKey);
            if (!string.IsNullOrEmpty(stored))
                foreach (var p in JsonSerializer.Deserialize<List<LimitProfile>>(stored) ?? new())
                {
                    p.BuiltIn = false;
                    Profiles.Add(p);
                }
            var settings = await _js.InvokeAsync<string?>("se.load", SettingsKey);
            var s = string.IsNullOrEmpty(settings) ? null : JsonSerializer.Deserialize<StoredSettings>(settings);
            AssemblerEfficiency = s?.AssemblerEfficiency ?? 1;
            YieldModules = s?.YieldModules ?? 0;
            RatePromptDone = s?.RatePromptDone ?? false;
            Active = Profiles.FirstOrDefault(p => p.Name == s?.ActiveProfile) ?? Profiles.First();
            Status = $"Vanilla data: {Db.Blocks.Count:N0} blocks · drop in blueprints to start";
        }
        catch (Exception ex)
        {
            Error = $"Could not load game data: {ex.Message}";
            Status = "Game data missing";
        }
        Notify();
    }

    public async Task AddBlueprintFilesAsync(IReadOnlyList<IBrowserFile> files, IReadOnlyList<string> paths)
    {
        Busy = true;
        Status = "Reading blueprints…";
        Notify();
        var groups = new Dictionary<string, (IBrowserFile? Bp, IBrowserFile? Thumb)>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < files.Count; i++)
        {
            var path = i < paths.Count && !string.IsNullOrEmpty(paths[i]) ? paths[i] : files[i].Name;
            var dir = path.Contains('/') ? path[..path.LastIndexOf('/')] : "";
            var name = files[i].Name.ToLowerInvariant();
            var key = name is "bp.sbc" or "thumb.png" ? dir + "|" : dir + "|" + name;
            groups.TryGetValue(key, out var g);
            if (name == "thumb.png") g.Thumb = files[i];
            else if (name.EndsWith(".sbc")) g.Bp = files[i];
            else continue;
            groups[key] = g;
        }

        var added = 0;
        foreach (var (key, g) in groups)
        {
            if (g.Bp == null) continue;
            var dir = key[..key.IndexOf('|')];
            var fallback = dir.Length > 0 ? dir[(dir.LastIndexOf('/') + 1)..] : Path.GetFileNameWithoutExtension(g.Bp.Name);
            var entry = await ReadEntryAsync(g.Bp, g.Thumb, fallback);
            Entries.RemoveAll(e => e.Name == entry.Name && e.Source == entry.Source);
            Entries.Insert(0, entry);
            added++;
            Status = $"Reading blueprints… {added}";
            Notify();
        }
        Selected ??= Entries.FirstOrDefault();
        Busy = false;
        Status = added == 0 ? "No bp.sbc found in the selection" : $"{Entries.Count} blueprints · {LoadedMods.Count} mods loaded";
        Notify();
    }

    async Task<WebEntry> ReadEntryAsync(IBrowserFile bp, IBrowserFile? thumb, string fallbackName)
    {
        string? thumbUrl = null;
        try
        {
            if (thumb != null && thumb.Size > 0 && thumb.Size < 20_000_000)
            {
                using var ms = new MemoryStream();
                await thumb.OpenReadStream(20_000_000).CopyToAsync(ms);
                thumbUrl = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }
        }
        catch (Exception) { }

        try
        {
            using var ms = new MemoryStream();
            await bp.OpenReadStream(MaxFileBytes).CopyToAsync(ms);
            ms.Position = 0;
            var data = BlueprintData.Parse(ms, fallbackName);
            var entry = new WebEntry { Name = data.Name, Data = data, Thumb = thumbUrl };
            Analyze(entry);
            return entry;
        }
        catch (Exception ex)
        {
            return new WebEntry { Name = fallbackName, Error = ex.Message, Thumb = thumbUrl };
        }
    }

    void Analyze(WebEntry e)
    {
        if (e.Data == null) return;
        try
        {
            e.Report = BlueprintAnalyzer.Analyze(e.Data, Db);
            e.Name = e.Report.Name;
            e.Error = null;
        }
        catch (Exception ex) { e.Error = ex.Message; }
    }

    public async Task AddModFilesAsync(IReadOnlyList<IBrowserFile> files, IReadOnlyList<string> paths)
    {
        Busy = true;
        Status = "Reading mods…";
        Notify();
        var builder = new GameDatabaseBuilder(Db);
        var mods = new HashSet<string>();
        for (var i = 0; i < files.Count; i++)
        {
            var path = (i < paths.Count ? paths[i] : files[i].Name).Replace('\\', '/');
            var parts = path.Split('/');
            var dataIndex = Array.FindIndex(parts, p => p.Equals("Data", StringComparison.OrdinalIgnoreCase));
            if (dataIndex < 1) continue;
            var lower = path.ToLowerInvariant();
            if (!lower.EndsWith(".sbc") && !(lower.EndsWith(".cs") && lower.Contains("/scripts/")) && !lower.EndsWith("mytexts.resx")) continue;
            var modId = parts[dataIndex - 1];
            var rel = string.Join('/', parts.Skip(dataIndex));
            try
            {
                using var ms = new MemoryStream();
                await files[i].OpenReadStream(MaxFileBytes).CopyToAsync(ms);
                var bytes = ms.ToArray();
                builder.AddModFile(modId, rel, () => new MemoryStream(bytes));
                mods.Add(modId);
            }
            catch (Exception) { }
            if (i % 25 == 0)
            {
                Status = $"Reading mods… {mods.Count} mods, {i + 1}/{files.Count} files";
                Notify();
                await Task.Yield();
            }
        }
        Db = builder.Build();
        foreach (var m in mods) if (!LoadedMods.Contains(m)) LoadedMods.Add(m);
        foreach (var e in Entries) Analyze(e);
        Busy = false;
        Status = mods.Count == 0 ? "No mod Data folders found in the selection" : $"{LoadedMods.Count} mods loaded · {Db.ModBlocks.Sum(m => m.Value.Count):N0} modded blocks";
        Notify();
    }

    public void ResetMods()
    {
        Db = _vanilla;
        LoadedMods.Clear();
        foreach (var e in Entries) Analyze(e);
        Status = "Mods removed · vanilla data only";
        Notify();
    }

    public void Remove(WebEntry e)
    {
        Entries.Remove(e);
        if (Selected == e) Selected = Entries.FirstOrDefault();
        Notify();
    }

    public async Task SetActiveAsync(LimitProfile p)
    {
        Active = p;
        await SaveSettingsAsync();
        Notify();
    }

    public async Task SetCostOptionsAsync(double assembler, int yieldModules)
    {
        AssemblerEfficiency = assembler;
        YieldModules = Math.Clamp(yieldModules, 0, 4);
        await SaveSettingsAsync();
        Notify();
    }

    public async Task FinishRatePromptAsync()
    {
        RatePromptDone = true;
        await SaveSettingsAsync();
    }

    public async Task SaveUserProfileAsync(LimitProfile updated, LimitProfile? original)
    {
        updated.BuiltIn = false;
        if (original != null) Profiles.Remove(original);
        var name = updated.Name;
        for (var i = 2; Profiles.Any(p => p.Name == updated.Name); i++) updated.Name = $"{name} {i}";
        Profiles.Add(updated);
        if (original == null || Active == original) Active = updated;
        await SaveProfilesAsync();
        await SaveSettingsAsync();
        Notify();
    }

    public async Task DeleteUserProfileAsync(LimitProfile p)
    {
        if (p.BuiltIn) return;
        Profiles.Remove(p);
        if (Active == p) Active = Profiles.First();
        await SaveProfilesAsync();
        await SaveSettingsAsync();
        Notify();
    }

    async Task SaveProfilesAsync() =>
        await _js.InvokeAsync<bool>("se.save", ProfilesKey, JsonSerializer.Serialize(Profiles.Where(p => !p.BuiltIn).ToList()));

    async Task SaveSettingsAsync() =>
        await _js.InvokeAsync<bool>("se.save", SettingsKey, JsonSerializer.Serialize(new StoredSettings
        {
            ActiveProfile = Active.Name,
            AssemblerEfficiency = AssemblerEfficiency,
            YieldModules = YieldModules,
            RatePromptDone = RatePromptDone,
        }));

    public void Notify() => Changed?.Invoke();

    static string ReadResource(string name)
    {
        using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                      ?? throw new InvalidOperationException($"Missing embedded resource {name}");
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    sealed class StoredSettings
    {
        public string? ActiveProfile { get; set; }
        public double AssemblerEfficiency { get; set; } = 1;
        public int YieldModules { get; set; }
        public bool RatePromptDone { get; set; }
    }
}
