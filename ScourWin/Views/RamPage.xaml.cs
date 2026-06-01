using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class RamPage : Page
{
    public RamViewModel Vm { get; } = new();
    private readonly DispatcherQueueTimer _timer;

    public RamPage()
    {
        InitializeComponent();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => Tick();
        Loaded += (_, _) => { Tick(); _timer.Start(); };
        Unloaded += (_, _) => _timer.Stop();
    }

    private void Tick()
    {
        Vm.Refresh();
        History.Push(Vm.UsedPercent);
    }
}
