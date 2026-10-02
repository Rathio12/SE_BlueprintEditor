using System.Collections.ObjectModel;
using System.Globalization;
using SEBlueprint.Core.Limits;

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
        _cargo = p.MaxCargoLiters is > 0 ? p.MaxCargoLiters.Value.ToString("0", CultureInfo.InvariantCulture) : "";
        foreach (var (k, v) in p.BlockTypeLimits.OrderBy(k => k.Key)) BlockLimits.Add(new BlockLimitRow { Key = k, Value = v });
    }

    public LimitProfile Original { get; }
    public bool IsReadOnly => Original.BuiltIn;
    public bool IsEditable => !Original.BuiltIn;
    public ObservableCollection<BlockLimitRow> BlockLimits { get; } = new();

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

    public LimitProfile ToProfile() => new()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "My profile" : Name.Trim(),
        TotalPcu = Int(TotalPcu),
        MaxPcuPerGrid = Int(MaxPcuPerGrid),
        Description = Original.BuiltIn ? null : Description,
        MaxBlocksPerGrid = Int(MaxBlocksPerGrid),
        MaxBlocksTotal = Int(MaxBlocksTotal),
        MaxGuns = Int(MaxGuns),
        MaxTurrets = Int(MaxTurrets),
        MaxCargoLiters = double.TryParse(MaxCargoLiters?.Replace(" ", ""), NumberStyles.Float, CultureInfo.CurrentCulture, out var c) && c > 0 ? c : null,
        BlockTypeLimits = BlockLimits.Where(b => !string.IsNullOrWhiteSpace(b.Key) && b.Value > 0)
            .GroupBy(b => b.Key.Trim()).ToDictionary(g => g.Key, g => g.Last().Value),
    };

    static string Str(int? v) => v is > 0 ? v.Value.ToString(CultureInfo.InvariantCulture) : "";

    static int? Int(string? s) =>
        int.TryParse(s?.Replace(" ", "").Replace(",", "").Replace(".", ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v > 0 ? v : null;
}
