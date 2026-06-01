using CommunityToolkit.Mvvm.ComponentModel;

namespace Scour.Models;

/// <summary>
/// A group of locations that can be cleaned as a unit (temp files, a dev tool's cache, etc.).
/// Used by the Temp Files and Dev Cleanup pages.
/// </summary>
public partial class CleanupCategory : ObservableObject
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";

    /// <summary>Directories whose *contents* are cleaned (the folders themselves are kept).</summary>
    public List<string> Paths { get; init; } = new();

    /// <summary>True to delete the matched directories entirely instead of just their contents.</summary>
    public bool RemoveRootToo { get; init; }

    [ObservableProperty] private long _size;
    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private bool _isScanning;

    /// <summary>Largest size in the current group, so the proportion bar can scale to it.</summary>
    [ObservableProperty] private long _groupMax = 1;

    public string SizeDisplay => Size <= 0 ? "empty" : Util.FormatBytes(Size);

    // Double projections for binding to numeric (double) control properties.
    public double SizeValue => Size;
    public double GroupMaxValue => GroupMax;

    partial void OnSizeChanged(long value)
    {
        OnPropertyChanged(nameof(SizeDisplay));
        OnPropertyChanged(nameof(SizeValue));
    }

    partial void OnGroupMaxChanged(long value) => OnPropertyChanged(nameof(GroupMaxValue));

    /// <summary>Only the paths that actually exist on this machine.</summary>
    public IEnumerable<string> ExistingPaths => Paths.Where(p => !string.IsNullOrEmpty(p) && (Directory.Exists(p) || File.Exists(p)));
}
