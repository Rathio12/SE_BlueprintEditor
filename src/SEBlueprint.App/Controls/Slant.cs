using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SEBlueprint.App.Controls;

public sealed class Slant : Decorator
{
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(Slant),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(Slant),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(nameof(Offset), typeof(double), typeof(Slant),
        new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public Brush? Stroke { get => (Brush?)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

    public double Offset { get => (double)GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    protected override Size MeasureOverride(Size constraint)
    {
        Child?.Measure(new Size(Math.Max(0, constraint.Width - Offset * 2), constraint.Height));
        var d = Child?.DesiredSize ?? default;
        return new Size(d.Width + Offset * 2, d.Height);
    }

    protected override Size ArrangeOverride(Size size)
    {
        Child?.Arrange(new Rect(Offset, 0, Math.Max(0, size.Width - Offset * 2), size.Height));
        return size;
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight, o = Offset;
        if (w <= 0 || h <= 0) return;
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            c.BeginFigure(new Point(o, 0.5), true, true);
            c.LineTo(new Point(w - 0.5, 0.5), true, false);
            c.LineTo(new Point(w - o, h - 0.5), true, false);
            c.LineTo(new Point(0.5, h - 0.5), true, false);
        }
        g.Freeze();
        dc.DrawGeometry(Fill, Stroke == null ? null : new Pen(Stroke, 1), g);
    }
}
