using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Scour.Models;
using Scour.Services;

namespace Scour.ViewModels;

public partial class FileSearchViewModel : ObservableObject
{
    private readonly FileSearchService _search = new();
    private readonly SettingsService _settings;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _cts;

    public ObservableCollection<FileItem> Results { get; } = new();

    [ObservableProperty] private string _root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    [ObservableProperty] private string _query = "";
    [ObservableProperty] private bool _computeSize;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string _statusText = "Choose a folder and enter a name to search for.";

    public FileSearchViewModel(SettingsService settings)
    {
        _settings = settings;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _cts?.Cancel();
        if (string.IsNullOrWhiteSpace(Query)) { StatusText = "Enter a search term."; return; }
        _cts = new CancellationTokenSource();
        IsSearching = true;
        Results.Clear();
        StatusText = $"Searching {Root}...";
        int n = 0;
        try
        {
            await _search.SearchAsync(Root, Query, ComputeSize,
                item => { n++; _dispatcher.TryEnqueue(() => Results.Add(item)); }, _cts.Token);
            StatusText = $"{Results.Count} result(s).";
        }
        catch (OperationCanceledException) { StatusText = "Search cancelled."; }
        finally { IsSearching = false; }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    public async Task<RemovalService.Result> RemoveSelectedAsync()
    {
        var remover = new RemovalService(_settings.Current);
        var result = await remover.RemoveAsync(new AppInfo { DisplayName = "File search" }, Results, runUninstaller: false);
        foreach (var i in Results.Where(i => i.IsSelected && !File.Exists(i.Path) && !Directory.Exists(i.Path)).ToList())
            Results.Remove(i);
        StatusText = $"Removed {result.Removed}, freed {Util.FormatBytes(result.BytesFreed)}.";
        return result;
    }
}
