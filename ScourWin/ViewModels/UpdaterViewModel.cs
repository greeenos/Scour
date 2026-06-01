using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Models;
using Scour.Services;

namespace Scour.ViewModels;

public partial class UpdaterViewModel : ObservableObject
{
    private readonly WingetService _winget = new();
    public ObservableCollection<UpdatablePackage> Packages { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Press Check for updates.";

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (!_winget.IsAvailable)
        {
            StatusText = "winget is not available. Install 'App Installer' from the Microsoft Store.";
            return;
        }
        IsBusy = true;
        StatusText = "Checking for updates...";
        Packages.Clear();
        var pkgs = await _winget.ListUpdatesAsync();
        foreach (var p in pkgs) Packages.Add(p);
        StatusText = pkgs.Count == 0 ? "Everything is up to date." : $"{pkgs.Count} update(s) available.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task UpdateSelectedAsync()
    {
        var sel = Packages.Where(p => p.IsSelected).ToList();
        if (sel.Count == 0) return;
        IsBusy = true;
        foreach (var p in sel)
        {
            p.IsUpdating = true;
            p.StatusText = "Updating...";
            var ok = await _winget.UpdateAsync(p.Id);
            p.StatusText = ok ? "Updated" : "Failed";
            p.IsUpdating = false;
        }
        StatusText = "Update run finished. Re-check to confirm.";
        IsBusy = false;
    }

    [RelayCommand] private void SelectAll() { foreach (var p in Packages) p.IsSelected = true; }
    [RelayCommand] private void DeselectAll() { foreach (var p in Packages) p.IsSelected = false; }
}
