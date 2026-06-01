using CommunityToolkit.Mvvm.ComponentModel;

namespace Scour.Models;

/// <summary>A Windows service row for the Services page (analogue of Scour's Daemon/Services manager).</summary>
public partial class ServiceItem : ObservableObject
{
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";

    [ObservableProperty] private string _status = "";
    [ObservableProperty] private bool _isBusy;

    public bool IsRunning => Status.Equals("Running", StringComparison.OrdinalIgnoreCase);

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(ActionLabel));
    }

    /// <summary>Label for the start/stop toggle button.</summary>
    public string ActionLabel => IsRunning ? "Stop" : "Start";
}
