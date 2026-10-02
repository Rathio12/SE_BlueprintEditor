namespace SEBlueprint.Web.Components;

public static class ItemArt
{
    public sealed record Palette(string Fill, string Light, string Edge);

    static readonly Dictionary<string, Palette> Materials = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Iron"] = new("#7D8A93", "#B4C0C7", "#3A454C"),
        ["Nickel"] = new("#8F8A6E", "#C9C29A", "#45422F"),
        ["Cobalt"] = new("#3E6FB0", "#7FA8E0", "#1F3A60"),
        ["Silicon"] = new("#5A6470", "#8E9AA8", "#2A3038"),
        ["Silver"] = new("#B9C2CA", "#E6ECF0", "#5E666D"),
        ["Gold"] = new("#D1A23A", "#F2CF74", "#6E5014"),
        ["Platinum"] = new("#A7B4C2", "#DCE6EF", "#55616D"),
        ["Uranium"] = new("#4E9A4A", "#8CD486", "#22501F"),
        ["Magnesium"] = new("#C8CCC2", "#F0F2EC", "#6A6E64"),
        ["Stone"] = new("#7A6E62", "#A89C8E", "#3C352E"),
        ["Scrap"] = new("#6E5A4A", "#9C8270", "#33281F"),
        ["Ice"] = new("#8FD3E8", "#D6F3FB", "#3E7F92"),
    };

    static readonly Palette Steel = new("#6F8796", "#A9BDC9", "#2F3E47");
    static readonly Palette Tech = new("#2D6F8F", "#7FC4E6", "#143847");
    static readonly Palette Warm = new("#B07A3A", "#E2AE6C", "#5A3A16");

    public static string ShapeOf(string id)
    {
        var (type, sub) = Split(id);
        if (type.Equals("Ingot", StringComparison.OrdinalIgnoreCase)) return "ingot";
        if (type.Equals("Ore", StringComparison.OrdinalIgnoreCase)) return "ore";
        var s = sub.ToLowerInvariant();
        if (s.Contains("plate")) return "plate";
        if (s.Contains("tube")) return "tube";
        if (s.Contains("grid")) return "grid";
        if (s.Contains("glass")) return "glass";
        if (s.Contains("display")) return "screen";
        if (s.Contains("motor")) return "motor";
        if (s.Contains("power") || s.Contains("solar") || s.Contains("superconductor") || s.Contains("thrust") || s.Contains("reactor")) return "cell";
        if (s.Contains("computer") || s.Contains("detector") || s.Contains("radio") || s.Contains("chip") || s.Contains("wafer")) return "chip";
        return "box";
    }

    public static Palette ColorOf(string id)
    {
        var (type, sub) = Split(id);
        if (type.Equals("Ingot", StringComparison.OrdinalIgnoreCase) || type.Equals("Ore", StringComparison.OrdinalIgnoreCase))
            return Materials.TryGetValue(sub, out var m) ? m : Steel;
        var s = sub.ToLowerInvariant();
        if (s.Contains("superconductor") || s.Contains("thrust") || s.Contains("reactor")) return Warm;
        if (s.Contains("computer") || s.Contains("detector") || s.Contains("radio") || s.Contains("display") || s.Contains("solar") || s.Contains("power")) return Tech;
        return Steel;
    }

    static (string Type, string Sub) Split(string id)
    {
        var i = id.IndexOf('/');
        return i < 0 ? (id, id) : (id[..i], id[(i + 1)..]);
    }
}
