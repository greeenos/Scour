using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class DevPage : Page
{
    public DevViewModel Vm { get; } = new();
    public DevPage() => InitializeComponent();
}
