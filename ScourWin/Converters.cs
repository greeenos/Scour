using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Scour.Models;

namespace Scour;

/// <summary>true -> Visible, false -> Collapsed. Pass "Invert" as parameter to flip.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b = value is bool v && v;
        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase)) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility vis && vis == Visibility.Visible;
}

/// <summary>true -> green (running), false -> muted gray. For service status dots.</summary>
public sealed class BoolToStatusBrushConverter : IValueConverter
{
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush Running =
        new(Windows.UI.Color.FromArgb(255, 52, 199, 89));
    private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush Stopped =
        new(Windows.UI.Color.FromArgb(255, 142, 142, 147));

    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b && b ? Running : Stopped;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

/// <summary>Inverts a boolean (e.g. enable a button when NOT busy).</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value is bool b && !b;
    public object ConvertBack(object value, Type targetType, object parameter, string language) => value is bool b && !b;
}

/// <summary>Maps a LeftoverKind to a Segoe Fluent Icons glyph.</summary>
public sealed class KindToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        LeftoverKind.InstallDirectory => "", // folder (app)
        LeftoverKind.Folder => "",
        LeftoverKind.File => "",            // document
        LeftoverKind.RegistryKey => "",     // gear/registry
        LeftoverKind.Shortcut => "",        // link
        LeftoverKind.Service => "",         // components
        LeftoverKind.ScheduledTask => "",   // clock
        _ => ""
    };
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

/// <summary>Returns Visible when the value is a non-empty string or any non-null object.</summary>
public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool visible = value switch
        {
            null => false,
            string s => !string.IsNullOrWhiteSpace(s),
            _ => true
        };
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
