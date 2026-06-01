using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel Vm { get; } = new();

    public HistoryPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Vm.LoadCommand.Execute(null);
    }

    private void OnOpenRecycleBin(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder") { UseShellExecute = true }); }
        catch { }
    }
}
