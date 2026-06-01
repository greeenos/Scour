using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scour.Services;

namespace Scour.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly RamCleaner _ram = new();
    private readonly InstalledAppsService _apps = new();
    private readonly TempCleaner _temp = new();

    [ObservableProperty] private double _ramPercent;
    [ObservableProperty] private string _ramCaption = "in use";
    [ObservableProperty] private string _ramDetail = "";

    [ObservableProperty] private double _diskPercent;
    [ObservableProperty] private string _diskDetail = "";
    [ObservableProperty] private string _diskDrive = "";

    [ObservableProperty] private string _appCount = "—";
    [ObservableProperty] private string _reclaimable = "—";
    [ObservableProperty] private bool _isScanningTemp;

    public void RefreshFast()
    {
        var m = _ram.GetStats();
        if (m.TotalBytes > 0)
        {
            RamPercent = m.LoadPercent;
            RamDetail = $"{Util.FormatBytes((long)m.UsedBytes)} of {Util.FormatBytes((long)m.TotalBytes)}";
        }

        try
        {
            var sys = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
            var drive = new DriveInfo(sys);
            if (drive.IsReady)
            {
                long used = drive.TotalSize - drive.TotalFreeSpace;
                DiskPercent = Math.Round((double)used / drive.TotalSize * 100);
                DiskDrive = drive.Name.TrimEnd('\\');
                DiskDetail = $"{Util.FormatBytes(drive.TotalFreeSpace)} free of {Util.FormatBytes(drive.TotalSize)}";
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        RefreshFast();

        var count = await Task.Run(() => _apps.GetInstalledApps().Count);
        AppCount = count.ToString();

        IsScanningTemp = true;
        var bytes = await Task.Run(() =>
        {
            long total = 0;
            foreach (var c in _temp.BuildCategories())
                total += c.ExistingPaths.Sum(FsUtil.SizeOf);
            return total;
        });
        Reclaimable = Util.FormatBytes(bytes);
        IsScanningTemp = false;
    }
}
