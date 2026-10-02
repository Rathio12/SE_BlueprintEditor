using System.Windows;
using System.Windows.Media;

namespace SEBlueprint.App.Controls;

/// <summary>A segmented gauge like Space Engineers' energy / hydrogen bars. Value is 0–1; above 1 every segment glows in OverBrush.</summary>
public sealed class SegmentBar : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(SegmentBar),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(nameof(Segments), typeof(int), typeof(SegmentBar),
        new FrameworkPropertyMetadata(20, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(SegmentBar),
        new FrameworkPropertyMetadata(Brushes.DeepSkyBlue, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(nameof(Track), typeof(Brush), typeof(SegmentBar),
        new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x14, 0x30, 0x3C)), FrameworkPropertyMetadataOptions.AffectsRender));

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public int Segments { get => (int)GetValue(SegmentsProperty); set => SetValue(SegmentsProperty, value); }
    public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public Brush Track { get => (Brush)GetValue(TrackProperty); set => SetValue(TrackProperty, value); }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 120 : availableSize.Width, 8);

    protected override void OnRender(DrawingContext dc)
    {
        var n = Math.Max(1, Segments);
        const double gap = 2;
        var w = (ActualWidth - gap * (n - 1)) / n;
        if (w <= 0) return;
        var lit = Value <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(Math.Min(1, Value) * n));
        for (var i = 0; i < n; i++)
        {
            var x = i * (w + gap);
            dc.DrawRectangle(i < lit ? Fill : Track, null, new Rect(x, 0, w, ActualHeight));
        }
    }
}
