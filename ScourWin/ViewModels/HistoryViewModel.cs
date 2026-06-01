using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Services;

namespace Scour.ViewModels;

public partial class HistoryRow
{
    public string AppName { get; init; } = "";
    public string When { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Details { get; init; } = "";
}

public partial class HistoryViewModel : ObservableObject
{
    public ObservableCollection<HistoryRow> Entries { get; } = new();

    [ObservableProperty] private string _statusText = "";

    public HistoryViewModel() => Load();

    [RelayCommand]
    public void Load()
    {
        Entries.Clear();
        var all = HistoryService.Shared.Load();
        foreach (var e in all)
        {
            Entries.Add(new HistoryRow
            {
                AppName = e.AppName,
                When = e.TimestampUtc.ToLocalTime().ToString("g"),
                Summary = $"{e.ItemCount} item(s) • {Util.FormatBytes(e.BytesFreed)} • {(e.Recycled ? "Recycle Bin" : "deleted")}",
                Details = string.Join(Environment.NewLine, e.Paths)
            });
        }
        StatusText = Entries.Count == 0 ? "No removals recorded yet." : $"{Entries.Count} removal session(s).";
    }

    [RelayCommand]
    private void Clear()
    {
        HistoryService.Shared.Clear();
        Load();
    }
}
