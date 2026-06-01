using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Scour.Controls;

/// <summary>A thin horizontal proportion bar (Value relative to Maximum). Used to visualize the
/// relative size of each cache/category in a list.</summary>
public sealed partial class UsageBar : UserControl
{
    public UsageBar()
    {
        InitializeComponent();
        Track.Opacity = 0.25;
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(UsageBar), new PropertyMetadata(0.0, (d, _) => ((UsageBar)d).Redraw()));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(UsageBar), new PropertyMetadata(1.0, (d, _) => ((UsageBar)d).Redraw()));
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
        nameof(BarBrush), typeof(Brush), typeof(UsageBar), new PropertyMetadata(null));
    public Brush? BarBrush { get => (Brush?)GetValue(BarBrushProperty); set => SetValue(BarBrushProperty, value); }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        if (Fill is null) return;
        double w = Track.ActualWidth;
        if (w <= 0) return;
        double frac = Maximum <= 0 ? 0 : Math.Clamp(Value / Maximum, 0, 1);
        Fill.Width = w * frac;
    }
}
