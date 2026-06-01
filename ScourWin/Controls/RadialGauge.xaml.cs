using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Scour.Controls;

/// <summary>
/// A donut-style radial gauge with a smoothly animated value arc and centered text.
/// Hand-drawn (no charting dependency) so it stays light and themable.
/// </summary>
public sealed partial class RadialGauge : UserControl
{
    private readonly DispatcherQueueTimer _timer;
    private double _displayed;       // currently rendered value
    private double _animFrom, _animTo;
    private DateTime _animStart;
    private static readonly TimeSpan AnimDuration = TimeSpan.FromMilliseconds(700);

    public RadialGauge()
    {
        InitializeComponent();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += (_, _) => Step();
        Loaded += (_, _) => Redraw();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RadialGauge),
        new PropertyMetadata(0.0, (d, _) => ((RadialGauge)d).AnimateTo()));

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RadialGauge),
        new PropertyMetadata(100.0, (d, _) => ((RadialGauge)d).Redraw()));

    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
        nameof(Diameter), typeof(double), typeof(RadialGauge),
        new PropertyMetadata(160.0, (d, _) => ((RadialGauge)d).Redraw()));

    public double Diameter { get => (double)GetValue(DiameterProperty); set => SetValue(DiameterProperty, value); }

    public static readonly DependencyProperty RingThicknessProperty = DependencyProperty.Register(
        nameof(RingThickness), typeof(double), typeof(RadialGauge),
        new PropertyMetadata(14.0, (d, _) => ((RadialGauge)d).Redraw()));

    public double RingThickness { get => (double)GetValue(RingThicknessProperty); set => SetValue(RingThicknessProperty, value); }

    public static readonly DependencyProperty GaugeForegroundProperty = DependencyProperty.Register(
        nameof(GaugeForeground), typeof(Brush), typeof(RadialGauge),
        new PropertyMetadata(null));

    public Brush? GaugeForeground { get => (Brush?)GetValue(GaugeForegroundProperty); set => SetValue(GaugeForegroundProperty, value); }

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(RadialGauge),
        new PropertyMetadata("", (d, _) => ((RadialGauge)d).UpdateText()));

    public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>Optional explicit center text; when null the percentage of Maximum is shown.</summary>
    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText), typeof(string), typeof(RadialGauge),
        new PropertyMetadata(null, (d, _) => ((RadialGauge)d).UpdateText()));

    public string? ValueText { get => (string?)GetValue(ValueTextProperty); set => SetValue(ValueTextProperty, value); }

    private void AnimateTo()
    {
        _animFrom = _displayed;
        _animTo = Value;
        _animStart = DateTime.UtcNow;
        if (!_timer.IsRunning) _timer.Start();
    }

    private void Step()
    {
        var t = (DateTime.UtcNow - _animStart).TotalMilliseconds / AnimDuration.TotalMilliseconds;
        if (t >= 1) { t = 1; _timer.Stop(); }
        // Ease-out cubic.
        var eased = 1 - Math.Pow(1 - t, 3);
        _displayed = _animFrom + (_animTo - _animFrom) * eased;
        Redraw();
    }

    private void UpdateText()
    {
        if (CenterValue is null) return;
        CenterValue.Text = ValueText ?? $"{Math.Round(_displayed / Math.Max(Maximum, 0.0001) * 100)}%";
        CenterCaption.Text = Caption;
    }

    private void Redraw()
    {
        if (Track is null || Diameter <= 0) return;

        double d = Diameter, t = RingThickness;
        double r = (d - t) / 2;
        double cx = d / 2, cy = d / 2;

        Track.Width = d - t;
        Track.Height = d - t;

        double frac = Maximum <= 0 ? 0 : Math.Clamp(_displayed / Maximum, 0, 1);
        if (frac <= 0)
        {
            ValueArc.Data = null;
            UpdateText();
            return;
        }

        double sweep = 360.0 * frac;
        if (sweep >= 360) sweep = 359.999;

        double start = -90 * Math.PI / 180.0;
        double end = (-90 + sweep) * Math.PI / 180.0;

        var startPt = new Point(cx + r * Math.Cos(start), cy + r * Math.Sin(start));
        var endPt = new Point(cx + r * Math.Cos(end), cy + r * Math.Sin(end));

        var figure = new PathFigure { StartPoint = startPt, IsClosed = false };
        figure.Segments.Add(new ArcSegment
        {
            Point = endPt,
            Size = new Size(r, r),
            IsLargeArc = sweep > 180,
            SweepDirection = SweepDirection.Clockwise
        });
        var geo = new PathGeometry();
        geo.Figures.Add(figure);
        ValueArc.Data = geo;

        UpdateText();
    }
}
