using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Models;
using Scour.Services;

namespace Scour.ViewModels;

/// <summary>Drives the Orphaned Files page (reverse scan).</summary>
public partial class OrphansViewModel : ObservableObject
{
    private readonly InstalledAppsService _appsService = new();
    private readonly SettingsService _settings;

    public ObservableCollection<FileItem> Orphans { get; } = new();

    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isRemoving;
    [ObservableProperty] private string _statusText = "Press Scan to find files left behind by uninstalled apps.";
    [ObservableProperty] private long _selectedSize;

    public string SelectedSizeDisplay => Util.FormatBytes(SelectedSize);
    partial void OnSelectedSizeChanged(long value) => OnPropertyChanged(nameof(SelectedSizeDisplay));

    public OrphansViewModel(SettingsService settings) => _settings = settings;

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsScanning = true;
        Orphans.Clear();
        StatusText = "Scanning for orphaned files...";
        try
        {
            var installed = await Task.Run(() => _appsService.GetInstalledApps());
            var scanner = new ReverseScanner(_settings.Current);
            var items = await scanner.ScanAsync(installed);
            foreach (var i in items)
            {
                i.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(FileItem.IsSelected)) Recalc(); };
                Orphans.Add(i);
            }
            Recalc();
            StatusText = $"Found {Orphans.Count} orphaned folders ({Util.FormatBytes(items.Sum(x => x.Size))}). Review carefully before removing.";
        }
        catch (Exception ex) { StatusText = $"Scan error: {ex.Message}"; }
        finally { IsScanning = false; }
    }

    private void Recalc() => SelectedSize = Orphans.Where(i => i.IsSelected).Sum(i => i.Size);

    [RelayCommand] private void SelectAll() { foreach (var i in Orphans) i.IsSelected = true; }
    [RelayCommand] private void DeselectAll() { foreach (var i in Orphans) i.IsSelected = false; }

    public async Task<RemovalService.Result> RemoveAsync()
    {
        IsRemoving = true;
        try
        {
            var remover = new RemovalService(_settings.Current);
            var dummy = new AppInfo { DisplayName = "Orphaned files" };
            var result = await remover.RemoveAsync(dummy, Orphans, runUninstaller: false);
            foreach (var i in Orphans.Where(i => i.IsSelected && !Directory.Exists(i.Path)).ToList())
                Orphans.Remove(i);
            Recalc();
            StatusText = $"Removed {result.Removed}, freed {Util.FormatBytes(result.BytesFreed)}.";
            return result;
        }
        finally { IsRemoving = false; }
    }
}
