using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SEBlueprint.App.ViewModels;
using SEBlueprint.Core.Limits;
using SEBlueprint.Core.Presentation;

namespace SEBlueprint.App.Converters;

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
        RowStatus.Ok or LimitStatus.Ok or Tone.Ok => Ok,
        RowStatus.Warn or LimitStatus.Warn or Tone.Warn or RowStatus.Incomplete => Warn,
        RowStatus.Over or LimitStatus.Over or RowStatus.Error or Tone.Over => Over,
        Tone.Accent => Accent,
        _ => Neutral,
    };
}

public sealed class StatusBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => StatusColors.For(value);
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class UnitConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return "—";
        var v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return Units.Format(v, parameter as string ?? "int");
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
