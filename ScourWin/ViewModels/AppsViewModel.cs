using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Models;
using Scour.Services;

namespace Scour.ViewModels;

/// <summary>
/// Drives the Applications page: lists installed apps, scans the selected app for leftovers,
/// and removes selected items. Windows analogue of AppState + the Applications view.
/// </summary>
public partial class AppsViewModel : ObservableObject
{
    private readonly InstalledAppsService _appsService = new();
    private readonly SettingsService _settings;
    private List<AppInfo> _allApps = new();
    private CancellationTokenSource? _scanCts;

    public ObservableCollection<AppInfo> Apps { get; } = new();
    public ObservableCollection<FileItem> Leftovers { get; } = new();

    [ObservableProperty] private AppInfo? _selectedApp;
    [ObservableProperty] private bool _isLoadingApps;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isRemoving;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _statusText = "Loading installed applications...";
    [ObservableProperty] private long _selectedLeftoverSize;
    [ObservableProperty] private string _appCountText = "";

    public AppsViewModel(SettingsService settings) => _settings = settings;

    public string SelectedLeftoverSizeDisplay => Util.FormatBytes(SelectedLeftoverSize);

    partial void OnSelectedLeftoverSizeChanged(long value) => OnPropertyChanged(nameof(SelectedLeftoverSizeDisplay));

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedAppChanged(AppInfo? value)
    {
        if (value is not null) _ = ScanAsync(value);
        else Leftovers.Clear();
    }

    [RelayCommand]
    public async Task LoadAppsAsync()
    {
        IsLoadingApps = true;
        StatusText = "Loading installed applications...";
        try
        {
            _allApps = await Task.Run(() => _appsService.GetInstalledApps());
            ApplyFilter();
            AppCountText = $"{_allApps.Count} applications";
            StatusText = $"Loaded {_allApps.Count} applications.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading apps: {ex.Message}";
        }
        finally { IsLoadingApps = false; }
    }

    private void ApplyFilter()
    {
        var q = SearchText.Trim();
        IEnumerable<AppInfo> filtered = _allApps;
        if (q.Length > 0)
            filtered = _allApps.Where(a =>
                a.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Publisher.Contains(q, StringComparison.OrdinalIgnoreCase));

        Apps.Clear();
        foreach (var a in filtered) Apps.Add(a);
    }

    private async Task ScanAsync(AppInfo app)
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        IsScanning = true;
        Leftovers.Clear();
        StatusText = $"Scanning for files related to {app.DisplayName}...";
        try
        {
            var scanner = new LeftoverScanner(_settings.Current);
            var items = await scanner.ScanAsync(app, ct);
            if (ct.IsCancellationRequested) return;

            foreach (var i in items)
            {
                i.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(FileItem.IsSelected)) RecalcSelected(); };
                Leftovers.Add(i);
            }
            RecalcSelected();
            StatusText = $"Found {Leftovers.Count} items ({Util.FormatBytes(items.Sum(x => x.Size))}).";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { StatusText = $"Scan error: {ex.Message}"; }
        finally { if (!ct.IsCancellationRequested) IsScanning = false; }
    }

    private void RecalcSelected() => SelectedLeftoverSize = Leftovers.Where(i => i.IsSelected).Sum(i => i.Size);

    [RelayCommand]
    private void SelectAll() { foreach (var i in Leftovers) i.IsSelected = true; }

    [RelayCommand]
    private void DeselectAll() { foreach (var i in Leftovers) i.IsSelected = false; }

    /// <summary>Removes selected leftovers. The confirmation dialog is handled by the view.</summary>
    public async Task<RemovalService.Result?> RemoveAsync(bool runUninstaller)
    {
        if (SelectedApp is null) return null;
        IsRemoving = true;
        try
        {
            var remover = new RemovalService(_settings.Current);
            var result = await remover.RemoveAsync(SelectedApp, Leftovers, runUninstaller);

            // Drop successfully removed items from the list (those no longer on disk).
            var toRemove = Leftovers.Where(i => i.IsSelected && !PathStillExists(i)).ToList();
            foreach (var i in toRemove) Leftovers.Remove(i);
            RecalcSelected();
            StatusText = $"Removed {result.Removed} items, freed {Util.FormatBytes(result.BytesFreed)}.";
            return result;
        }
        finally { IsRemoving = false; }
    }

    private static bool PathStillExists(FileItem i) =>
        i.Kind is LeftoverKind.RegistryKey or LeftoverKind.Service
            ? false
            : (File.Exists(i.Path) || Directory.Exists(i.Path));
}
