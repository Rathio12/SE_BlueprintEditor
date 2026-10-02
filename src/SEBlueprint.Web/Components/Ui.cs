using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Presentation;

namespace SEBlueprint.Web.Components;

public static class Ui
{
    public static string Color(Tone t) => t switch
    {
        Tone.Ok => "var(--green)",
        Tone.Warn => "var(--orange)",
        Tone.Over => "var(--red)",
        Tone.Neutral => "var(--text-faint)",
        _ => "var(--accent)",
    };

    public static string Css(Tone t) => t switch
    {
        Tone.Ok => "ok",
        Tone.Warn => "warn",
        Tone.Over => "over",
        Tone.Neutral => "neutral",
        _ => "accent",
    };

    public static Tone Of(LimitStatus s) => ReportView.ToneOf(s);

    public static string StatusText(LimitStatus s) => s switch
    {
        LimitStatus.Over => "Over limit",
        LimitStatus.Warn => "Near limit",
        LimitStatus.Ok => "OK",
        _ => "No limits",
    };

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}",
        };
    }
}
