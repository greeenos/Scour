using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class TempPage : Page
{
    public TempViewModel Vm { get; } = new();
    public TempPage() => InitializeComponent();
}
