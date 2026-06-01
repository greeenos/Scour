using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Scour.Controls;

/// <summary>
/// A lightweight live line chart. Push values over time; it auto-scrolls and draws a smooth line
/// with a translucent gradient area beneath. Hand-drawn — no charting dependency.
/// </summary>
public sealed partial class Sparkline : UserControl
{
    private readonly List<double> _values = new();

    public Sparkline()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty CapacityProperty = DependencyProperty.Register(
        nameof(Capacity), typeof(int), typeof(Sparkline), new PropertyMetadata(60));
    public int Capacity { get => (int)GetValue(CapacityProperty); set => SetValue(CapacityProperty, value); }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(Sparkline), new PropertyMetadata(100.0));
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush), typeof(Brush), typeof(Sparkline), new PropertyMetadata(null, (d, _) => ((Sparkline)d).ApplyBrushes()));
    public Brush? LineBrush { get => (Brush?)GetValue(LineBrushProperty); set => SetValue(LineBrushProperty, value); }

    public static readonly DependencyProperty AreaColorProperty = DependencyProperty.Register(
        nameof(AreaColor), typeof(Color), typeof(Sparkline), new PropertyMetadata(Color.FromArgb(255, 52, 199, 89), (d, _) => ((Sparkline)d).ApplyBrushes()));
    public Color AreaColor { get => (Color)GetValue(AreaColorProperty); set => SetValue(AreaColorProperty, value); }

    public void Push(double value)
    {
        _values.Add(value);
        while (_values.Count > Capacity) _values.RemoveAt(0);
        Redraw();
    }

    public void Clear() { _values.Clear(); Redraw(); }

    private void ApplyBrushes()
    {
        if (Line is null) return;
        Line.Stroke = LineBrush ?? new SolidColorBrush(AreaColor);
        var grad = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
        grad.GradientStops.Add(new GradientStop { Color = Color.FromArgb(120, AreaColor.R, AreaColor.G, AreaColor.B), Offset = 0 });
        grad.GradientStops.Add(new GradientStop { Color = Color.FromArgb(0, AreaColor.R, AreaColor.G, AreaColor.B), Offset = 1 });
        Area.Fill = grad;
        Redraw();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        if (Line is null) return;
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0 || _values.Count < 2)
        {
            Line.Points = new PointCollection();
            Area.Points = new PointCollection();
            return;
        }

        double max = Math.Max(Maximum, 1);
        double pad = 2;
        double usableH = h - pad * 2;
        int n = _values.Count;
        double stepX = w / (Capacity - 1);

        var pts = new PointCollection();
        // Right-align newest sample.
        double xOffset = w - (n - 1) * stepX;
        for (int i = 0; i < n; i++)
        {
            double x = xOffset + i * stepX;
            double frac = Math.Clamp(_values[i] / max, 0, 1);
            double y = pad + (1 - frac) * usableH;
            pts.Add(new Point(x, y));
        }
        Line.Points = pts;

        var area = new PointCollection();
        foreach (var p in pts) area.Add(p);
        area.Add(new Point(pts[^1].X, h));
        area.Add(new Point(pts[0].X, h));
        Area.Points = area;
    }
}
