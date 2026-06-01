using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.Models;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class ServicesPage : Page
{
    public ServicesViewModel Vm { get; } = new();

    public ServicesPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => { if (Vm.Services.Count == 0) await Vm.LoadCommand.ExecuteAsync(null); };
    }

    private void OnToggle(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ServiceItem item)
            Vm.ToggleCommand.Execute(item);
    }
}
