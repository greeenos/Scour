using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class UpdaterPage : Page
{
    public UpdaterViewModel Vm { get; } = new();
    public UpdaterPage() => InitializeComponent();
}
