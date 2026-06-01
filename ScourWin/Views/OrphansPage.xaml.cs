using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class OrphansPage : Page
{
    public OrphansViewModel Vm { get; } = new(App.Settings);

    public OrphansPage() => InitializeComponent();

    private async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        var count = Vm.Orphans.Count(i => i.IsSelected);
        if (count == 0) return;

        var binNote = App.Settings.Current.SendToRecycleBin
            ? "Items will be sent to the Recycle Bin."
            : "Items will be permanently deleted.";
        var dialog = new ContentDialog
        {
            Title = $"Remove {count} orphaned folder(s)?",
            Content = $"Make sure none of these belong to an app you still use.\n\n{binNote}",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        await Vm.RemoveAsync();
    }
}
