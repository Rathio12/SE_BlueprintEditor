using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SEBlueprint.App.ViewModels;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.App.Converters;

/// <summary>Colours for limit states, shared by chips, bars and badges.</summary>
public static class StatusColors
{
    public static readonly SolidColorBrush Ok = Make("#5BC07A");
    public static readonly SolidColorBrush Warn = Make("#F0A030");
    public static readonly SolidColorBrush Over = Make("#E5534B");
    public static readonly SolidColorBrush Neutral = Make("#5D7682");
    public static readonly SolidColorBrush Accent = Make("#46B4E6");

    static SolidColorBrush Make(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    public static SolidColorBrush For(object? value) => value switch
    {
        RowStatus.Ok or LimitStatus.Ok => Ok,
        RowStatus.Warn or LimitStatus.Warn => Warn,
        RowStatus.Over or LimitStatus.Over or RowStatus.Error => Over,
        _ => Neutral,
    };
}

public sealed class StatusBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => StatusColors.For(value);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Formats numbers for display: kind = liters | mass | newtons | int | mw | mwh | km.</summary>
public sealed class UnitConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "—";
        var v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return Format.Unit(v, parameter as string ?? "int");
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value switch { bool x => x, null => false, string s => s.Length > 0, int i => i != 0, _ => true };
        return b ^ Invert ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public static class Format
{
    static readonly CultureInfo C = CultureInfo.CurrentCulture;

    public static string Unit(double v, string kind) => kind switch
    {
        "liters" => v >= 1_000_000 ? $"{(v / 1_000_000).ToString("0.##", C)} ML" : v >= 10_000 ? $"{(v / 1000).ToString("#,0.#", C)} kL" : $"{v.ToString("#,0", C)} L",
        "mass" => v >= 1_000_000 ? $"{(v / 1_000_000).ToString("0.##", C)} kt" : v >= 10_000 ? $"{(v / 1000).ToString("#,0.#", C)} t" : $"{v.ToString("#,0", C)} kg",
        "newtons" => v >= 1_000_000 ? $"{(v / 1_000_000).ToString("0.##", C)} MN" : $"{(v / 1000).ToString("#,0", C)} kN",
        "mw" => $"{v.ToString("#,0.##", C)} MW",
        "mwh" => $"{v.ToString("#,0.##", C)} MWh",
        "km" => $"{v.ToString("#,0.#", C)} km",
        "amount" => v >= 100 ? v.ToString("#,0", C) : v.ToString("#,0.##", C),
        _ => v.ToString("#,0", C),
    };
}
