using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SEBlueprint.App.Controls;

public sealed class SeFrame : Decorator
{
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(SeFrame),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(SeFrame),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(SeFrame),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ChamferProperty = DependencyProperty.Register(nameof(Chamfer), typeof(double), typeof(SeFrame),
        new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty CutAllCornersProperty = DependencyProperty.Register(nameof(CutAllCorners), typeof(bool), typeof(SeFrame),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(nameof(Accent), typeof(Brush), typeof(SeFrame),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty BracketsProperty = DependencyProperty.Register(nameof(Brackets), typeof(Brush), typeof(SeFrame),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(SeFrame),
        new FrameworkPropertyMetadata(new Thickness(14), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Brush? Fill { get => (Brush?)GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public Brush? Stroke { get => (Brush?)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public double Chamfer { get => (double)GetValue(ChamferProperty); set => SetValue(ChamferProperty, value); }
    public bool CutAllCorners { get => (bool)GetValue(CutAllCornersProperty); set => SetValue(CutAllCornersProperty, value); }

    public Brush? Accent { get => (Brush?)GetValue(AccentProperty); set => SetValue(AccentProperty, value); }

    public Brush? Brackets { get => (Brush?)GetValue(BracketsProperty); set => SetValue(BracketsProperty, value); }
    public Thickness Padding { get => (Thickness)GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    protected override Size MeasureOverride(Size constraint)
    {
        var p = Padding;
        var inner = new Size(Math.Max(0, constraint.Width - p.Left - p.Right), Math.Max(0, constraint.Height - p.Top - p.Bottom));
        Child?.Measure(inner);
        var d = Child?.DesiredSize ?? default;
        return new Size(d.Width + p.Left + p.Right, d.Height + p.Top + p.Bottom);
    }

    protected override Size ArrangeOverride(Size size)
    {
        var p = Padding;
        Child?.Arrange(new Rect(p.Left, p.Top, Math.Max(0, size.Width - p.Left - p.Right), Math.Max(0, size.Height - p.Top - p.Bottom)));
        return size;
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        var t = StrokeThickness / 2;
        var c = Math.Min(Chamfer, Math.Min(w, h) / 3);
        var all = CutAllCorners;

        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            ctx.BeginFigure(new Point(t + c, t), true, true);
            ctx.LineTo(new Point(w - t - (all ? c : 0), t), true, false);
            if (all) ctx.LineTo(new Point(w - t, t + c), true, false);
            ctx.LineTo(new Point(w - t, h - t - c), true, false);
            ctx.LineTo(new Point(w - t - c, h - t), true, false);
            ctx.LineTo(new Point(t + (all ? c : 0), h - t), true, false);
            if (all) ctx.LineTo(new Point(t, h - t - c), true, false);
            ctx.LineTo(new Point(t, t + c), true, false);
        }
        g.Freeze();

        var pen = Stroke == null ? null : new Pen(Stroke, StrokeThickness);
        dc.DrawGeometry(Fill, pen, g);

        if (Brackets != null)
        {
            var bp = new Pen(Brackets, 2);
            const double L = 14, i = 1;

            dc.DrawLine(bp, new Point(w - i - L, i), new Point(w - i, i));
            dc.DrawLine(bp, new Point(w - i, i), new Point(w - i, i + L));
            dc.DrawLine(bp, new Point(i, h - i - L), new Point(i, h - i));
            dc.DrawLine(bp, new Point(i, h - i), new Point(i + L, h - i));

            dc.DrawLine(bp, new Point(i, c + i), new Point(c + i, i));
            dc.DrawLine(bp, new Point(w - i, h - c - i), new Point(w - c - i, h - i));
        }

        if (Accent != null)
        {
            var accent = new Pen(Accent, 2) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
            dc.DrawLine(accent, new Point(1, c + 1), new Point(c + 1, 1));
            dc.DrawLine(accent, new Point(c + 1, 1), new Point(Math.Min(w * 0.35, c + 90), 1));
        }
    }
}
