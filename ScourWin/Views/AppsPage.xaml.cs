using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;

namespace Scour.Views;

public sealed partial class AppsPage : Page
{
    public AppsViewModel Vm { get; }

    public AppsPage()
    {
        Vm = new AppsViewModel(App.Settings);
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (Vm.Apps.Count == 0) await Vm.LoadAppsCommand.ExecuteAsync(null);
        };
    }

    private async void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        if (Vm.SelectedApp is null) return;
        var selectedCount = Vm.Leftovers.Count(i => i.IsSelected);
        if (selectedCount == 0) return;

        bool runUninstaller = RunUninstallerCheck.IsChecked == true;

        if (App.Settings.Current.ConfirmBeforeRemoval)
        {
            var binNote = App.Settings.Current.SendToRecycleBin
                ? "Items will be sent to the Recycle Bin."
                : "Items will be permanently deleted.";
            var dialog = new ContentDialog
            {
                Title = $"Remove {selectedCount} item(s)?",
                Content = $"This will remove the selected leftovers for \"{Vm.SelectedApp.DisplayName}\".\n\n{binNote}",
                PrimaryButtonText = "Remove",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        }

        await Vm.RemoveAsync(runUninstaller);
    }
}
