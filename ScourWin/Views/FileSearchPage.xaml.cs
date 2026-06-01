using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.ViewModels;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Scour.Views;

public sealed partial class FileSearchPage : Page
{
    public FileSearchViewModel Vm { get; } = new(App.Settings);

    public FileSearchPage() => InitializeComponent();

    private async void OnBrowse(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        picker.FileTypeFilter.Add("*");
        // Unpackaged WinUI 3: associate the picker with our window handle.
        if (App.MainWindow is { } w)
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(w));
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null) Vm.Root = folder.Path;
    }

    private async void OnRemove(object sender, RoutedEventArgs e)
    {
        var count = Vm.Results.Count(i => i.IsSelected);
        if (count == 0) return;
        var dialog = new ContentDialog
        {
            Title = $"Remove {count} item(s)?",
            Content = App.Settings.Current.SendToRecycleBin ? "Items will be sent to the Recycle Bin." : "Items will be permanently deleted.",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        await Vm.RemoveSelectedAsync();
    }
}
