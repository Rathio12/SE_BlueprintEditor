using System.Collections.ObjectModel;
using System.Globalization;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Parsing;

namespace SEBlueprint.App.ViewModels;

public sealed class BlockLimitRow : ObservableObject
{
    string _key = "";
    int _value;
    public string Key { get => _key; set => Set(ref _key, value); }
    public int Value { get => _value; set => Set(ref _value, value); }
}

public sealed class ProfileEditorViewModel : ObservableObject
{
    string _name = "", _pcu = "", _pcuPerGrid = "", _perGrid = "", _total = "", _guns = "", _turrets = "", _cargo = "";

    public ProfileEditorViewModel(LimitProfile p)
    {
        Original = p;
        _name = p.Name;
        _pcu = Str(p.TotalPcu);
        _pcuPerGrid = Str(p.MaxPcuPerGrid);
        Description = p.Description;
        _perGrid = Str(p.MaxBlocksPerGrid);
        _total = Str(p.MaxBlocksTotal);
        _guns = Str(p.MaxGuns);
        _turrets = Str(p.MaxTurrets);
        _cargo = UserNumbers.Format(p.MaxCargoLiters);
        foreach (var (k, v) in p.BlockTypeLimits.OrderBy(k => k.Key)) BlockLimits.Add(new BlockLimitRow { Key = k, Value = v });
    }

    public LimitProfile Original { get; }
    public bool IsReadOnly => Original.BuiltIn;
    public bool IsEditable => !Original.BuiltIn;
    public ObservableCollection<BlockLimitRow> BlockLimits { get; } = new();
    public IReadOnlyList<GroupRuleRow> GroupRules => Original.GroupLimits.Select(g => new GroupRuleRow(g)).ToList();
    public bool HasGroupRules => Original.GroupLimits.Count > 0;

    public string Name { get => _name; set => Set(ref _name, value); }
    public string TotalPcu { get => _pcu; set => Set(ref _pcu, value); }
    public string MaxPcuPerGrid { get => _pcuPerGrid; set => Set(ref _pcuPerGrid, value); }
    public string? Description { get; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public string MaxBlocksPerGrid { get => _perGrid; set => Set(ref _perGrid, value); }
    public string MaxBlocksTotal { get => _total; set => Set(ref _total, value); }
    public string MaxGuns { get => _guns; set => Set(ref _guns, value); }
    public string MaxTurrets { get => _turrets; set => Set(ref _turrets, value); }
    public string MaxCargoLiters { get => _cargo; set => Set(ref _cargo, value); }

    public bool TryToProfile(out LimitProfile profile, out string? error)
    {
        profile = new LimitProfile();
        error = null;
        var culture = CultureInfo.CurrentCulture;
        var bad = new List<string>();
        int? Whole(string label, string text)
        {
            if (UserNumbers.TryParseLimit(text, culture, integer: true, out var v)) return v is null ? null : (int)v.Value;
            bad.Add(label);
            return null;
        }
        profile.Name = string.IsNullOrWhiteSpace(Name) ? "My profile" : Name.Trim();
        profile.Description = Original.BuiltIn ? null : Description;
        profile.GroupLimits = Original.GroupLimits.Select(g => g.Clone()).ToList();
        profile.TotalPcu = Whole("Max PCU", TotalPcu);
        profile.MaxPcuPerGrid = Whole("Max PCU per grid", MaxPcuPerGrid);
        profile.MaxBlocksPerGrid = Whole("Max blocks per grid", MaxBlocksPerGrid);
        profile.MaxBlocksTotal = Whole("Max blocks total", MaxBlocksTotal);
        profile.MaxGuns = Whole("Max guns", MaxGuns);
        profile.MaxTurrets = Whole("Max turrets", MaxTurrets);
        if (UserNumbers.TryParseLimit(MaxCargoLiters, culture, integer: false, out var cargo)) profile.MaxCargoLiters = cargo;
        else bad.Add("Max cargo");
        profile.BlockTypeLimits = BlockLimits.Where(b => !string.IsNullOrWhiteSpace(b.Key) && b.Value > 0)
            .GroupBy(b => b.Key.Trim()).ToDictionary(g => g.Key, g => g.Last().Value);
        if (bad.Count == 0) return true;
        error = $"Not saved — please enter a whole positive number (or leave empty for no limit) in: {string.Join(", ", bad)}.";
        return false;
    }

    static string Str(int? v) => UserNumbers.Format(v);

}

public sealed record GroupRuleRow(BlockGroupLimit Rule)
{
    public string Stat => Rule.Stat;
    public string Max => Rule.Max == 0 ? "not allowed" : UserNumbers.Format(Rule.Max);
    public string Blocks => string.Join(", ", Rule.Blocks);
}
