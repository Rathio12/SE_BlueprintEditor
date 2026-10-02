using System.Globalization;

namespace SEBlueprint.Core.Parsing;

public static class UserNumbers
{
    static readonly char[] GroupSeparators = { ' ', ' ', ' ', '\'', '’' };

    public static bool TryParseLimit(string? text, CultureInfo culture, bool integer, out double? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;

        var cleaned = new string(text.Trim().Where(c => Array.IndexOf(GroupSeparators, c) < 0).ToArray());
        const NumberStyles styles = NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint;
        if (!double.TryParse(cleaned, styles, culture, out var parsed) || double.IsInfinity(parsed) || parsed < 0)
            return false;
        if (integer && (parsed != Math.Floor(parsed) || parsed > int.MaxValue))
            return false;

        value = parsed == 0 ? null : parsed;
        return true;
    }

    public static string Format(double? value) =>
        value is > 0 ? value.Value.ToString("#,0.##", CultureInfo.CurrentCulture) : "";
}
