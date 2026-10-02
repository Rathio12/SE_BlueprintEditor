using System.Globalization;

namespace SEBlueprint.Core.Parsing;

/// <summary>Culture-independent number parsing for game definition files (always '.' decimals).</summary>
public static class Num
{
    const NumberStyles Float = NumberStyles.Float;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static double D(string? s, double fallback = 0) =>
        double.TryParse(s?.Trim(), Float, Inv, out var v) ? v : fallback;

    public static float F(string? s, float fallback = 0) =>
        float.TryParse(s?.Trim(), Float, Inv, out var v) ? v : fallback;

    public static int I(string? s, int fallback = 0) =>
        int.TryParse(s?.Trim(), NumberStyles.Integer, Inv, out var v) ? v : fallback;
}
