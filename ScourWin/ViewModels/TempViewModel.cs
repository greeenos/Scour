using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Models;
using Scour.Services;

namespace Scour.ViewModels;

public partial class TempViewModel : ObservableObject
{
    private readonly TempCleaner _cleaner = new();
    public ObservableCollection<CleanupCategory> Categories { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Press Scan to measure temporary files.";

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusText = "Scanning temp locations...";
        Categories.Clear();
        foreach (var c in _cleaner.BuildCategories()) Categories.Add(c);
        await _cleaner.SizeAsync(Categories);
        var max = Math.Max(1, Categories.Count == 0 ? 1 : Categories.Max(c => c.Size));
        foreach (var c in Categories) c.GroupMax = max;
        StatusText = $"Found {Util.FormatBytes(Categories.Sum(c => c.Size))} across {Categories.Count} categories.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        IsBusy = true;
        var (freed, skipped) = await _cleaner.CleanAsync(Categories);
        StatusText = $"Freed {Util.FormatBytes(freed)}" + (skipped > 0 ? $" ({skipped} in use/skipped)." : ".");
        IsBusy = false;
    }
}
