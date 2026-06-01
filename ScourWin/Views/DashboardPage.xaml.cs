using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel Vm { get; } = new();
    private readonly DispatcherQueueTimer _timer;

    public DashboardPage()
    {
        InitializeComponent();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.Tick += (_, _) => Vm.RefreshFast();
        Loaded += async (_, _) => { await Vm.LoadAsync(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void GoApps(object s, RoutedEventArgs e) => (App.MainWindow as MainWindow)?.NavigateTo("apps");
    private void GoTemp(object s, RoutedEventArgs e) => (App.MainWindow as MainWindow)?.NavigateTo("temp");
    private void GoRam(object s, RoutedEventArgs e) => (App.MainWindow as MainWindow)?.NavigateTo("ram");
    private void GoUpdater(object s, RoutedEventArgs e) => (App.MainWindow as MainWindow)?.NavigateTo("updater");
}
