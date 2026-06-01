using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Services;

namespace Scour.ViewModels;

public partial class RamViewModel : ObservableObject
{
    private readonly RamCleaner _cleaner = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private double _usedPercent;
    [ObservableProperty] private string _usedText = "";
    [ObservableProperty] private string _totalText = "";
    [ObservableProperty] private string _availableText = "";
    [ObservableProperty] private string _statusText = "";

    public RamViewModel() => Refresh();

    [RelayCommand]
    public void Refresh()
    {
        var s = _cleaner.GetStats();
        if (s.TotalBytes == 0) { StatusText = "Could not read memory stats."; return; }
        UsedPercent = s.LoadPercent;
        TotalText = Util.FormatBytes((long)s.TotalBytes);
        AvailableText = Util.FormatBytes((long)s.AvailableBytes);
        UsedText = Util.FormatBytes((long)s.UsedBytes);
        if (string.IsNullOrEmpty(StatusText))
            StatusText = $"{s.LoadPercent}% in use.";
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        IsBusy = true;
        StatusText = "Freeing memory...";
        var (freed, trimmed) = await _cleaner.CleanAsync();
        Refresh();
        StatusText = $"Trimmed {trimmed} processes, freed ~{Util.FormatBytes(freed)}. Run as admin to also purge the standby cache.";
        IsBusy = false;
    }
}
