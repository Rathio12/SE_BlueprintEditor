using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SEBlueprint.Core.Limits;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LimitScope { Grid, Player }

public sealed class BlockGroupLimit
{
    public string Name { get; set; } = "";
    public LimitScope Scope { get; set; } = LimitScope.Grid;
    public int Max { get; set; }
    public List<string> Blocks { get; set; } = new();

    public string Stat => $"{Name} {(Scope == LimitScope.Grid ? "per grid" : "per player")}";

    public BlockGroupLimit Clone() => new() { Name = Name, Scope = Scope, Max = Max, Blocks = new(Blocks) };

    internal string Key() => $"{Name}:{Scope}:{Max}:{string.Join(";", Blocks)}";
}

public static class BlockMatcher
{
    static readonly ConcurrentDictionary<string, Regex> Globs = new(StringComparer.Ordinal);

    public static bool Matches(string blockId, IEnumerable<string> patterns)
    {
        var included = false;
        foreach (var raw in patterns)
        {
            var pattern = raw.Trim();
            if (pattern.Length == 0) continue;
            if (pattern[0] == '!')
            {
                if (MatchOne(blockId, pattern[1..].Trim())) return false;
            }
            else if (!included && MatchOne(blockId, pattern)) included = true;
        }
        return included;
    }

    static bool MatchOne(string blockId, string pattern)
    {
        var slash = blockId.IndexOf('/');
        var type = slash < 0 ? blockId : blockId[..slash];
        var subtype = slash < 0 ? "" : blockId[(slash + 1)..];
        var p = pattern.IndexOf('/');
        if (p < 0) return Glob(pattern).IsMatch(type);
        return Glob(pattern[..p]).IsMatch(type) && Glob(pattern[(p + 1)..]).IsMatch(subtype);
    }

    static Regex Glob(string glob) => Globs.GetOrAdd(glob, g =>
        new Regex("^" + Regex.Escape(g).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)));
}
