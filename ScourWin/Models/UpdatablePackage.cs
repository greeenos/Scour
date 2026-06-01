using CommunityToolkit.Mvvm.ComponentModel;

namespace Scour.Models;

/// <summary>An app with an available update, parsed from `winget upgrade`. Drives the App Updater page.</summary>
public partial class UpdatablePackage : ObservableObject
{
    public string Name { get; init; } = "";
    public string Id { get; init; } = "";
    public string CurrentVersion { get; init; } = "";
    public string AvailableVersion { get; init; } = "";
    public string Source { get; init; } = "";

    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private bool _isUpdating;
    [ObservableProperty] private string _statusText = "";
}
