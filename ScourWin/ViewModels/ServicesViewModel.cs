using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Models;
using Scour.Services;

namespace Scour.ViewModels;

public partial class ServicesViewModel : ObservableObject
{
    private readonly ServicesManager _manager = new();
    private List<ServiceItem> _all = new();

    public ObservableCollection<ServiceItem> Services { get; } = new();

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _statusText = "Loading services...";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        StatusText = "Loading services...";
        _all = await _manager.ListAsync();
        ApplyFilter();
        StatusText = $"{_all.Count} services.";
        IsBusy = false;
    }

    private void ApplyFilter()
    {
        var q = SearchText.Trim();
        var filtered = q.Length == 0 ? _all :
            _all.Where(s => s.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                            s.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        Services.Clear();
        foreach (var s in filtered) Services.Add(s);
    }

    [RelayCommand]
    private async Task ToggleAsync(ServiceItem item)
    {
        if (item is null) return;
        item.IsBusy = true;
        await _manager.SetRunningAsync(item, start: !item.IsRunning);
        item.IsBusy = false;
    }
}
