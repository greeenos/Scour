using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Scour.Services;
using Scour.Views;

namespace Scour;

public sealed partial class MainWindow : Window
{
    private bool _exiting;

    /// <summary>Restores the window from the tray (bound to the tray icon's left click).</summary>
    public IRelayCommand ShowFromTrayCommand { get; }

    public MainWindow()
    {
        ShowFromTrayCommand = new RelayCommand(ShowFromTray);
        InitializeComponent();
        ConsoleManager.Shared.AttachDispatcher(DispatcherQueue.GetForCurrentThread());

        // Modern translucent window backdrop + content-into-titlebar for a clean, professional shell.
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        ApplyTheme(App.Settings.Current.Theme);
        Title = "Scour for Windows";

        try
        {
            var ico = Path.Combine(AppContext.BaseDirectory, "Assets", "scour.ico");
            if (File.Exists(ico)) AppWindow.SetIcon(ico);
        }
        catch { /* icon is cosmetic */ }

        // Close-to-tray: intercept the close button and hide instead of exit.
        AppWindow.Closing += (_, e) =>
        {
            if (!_exiting && App.Settings.Current.MinimizeToTray)
            {
                e.Cancel = true;
                AppWindow.Hide();
            }
        };

        // If auto-started at sign-in, drop straight to the tray once the shell has loaded.
        if (App.StartedHidden)
            Activated += HideOnFirstActivation;
    }

    private void HideOnFirstActivation(object sender, WindowActivatedEventArgs e)
    {
        Activated -= HideOnFirstActivation;
        DispatcherQueue.TryEnqueue(() => AppWindow.Hide());
    }

    private void ShowFromTray()
    {
        AppWindow.Show();
        Activate();
    }

    // ----- Tray menu -----
    private void Tray_Open(object sender, RoutedEventArgs e) => ShowFromTray();

    private async void Tray_FreeMemory(object sender, RoutedEventArgs e)
    {
        var (freed, _) = await new RamCleaner().CleanAsync();
        ConsoleManager.Shared.Append($"Tray: freed ~{Util.FormatBytes(freed)} of memory.");
    }

    private void Tray_Updates(object sender, RoutedEventArgs e)
    {
        ShowFromTray();
        NavigateTo("updater");
    }

    private void Tray_Exit(object sender, RoutedEventArgs e)
    {
        _exiting = true;
        try { TrayIcon.Dispose(); } catch { }
        Close();
    }

    private void Nav_Loaded(object sender, RoutedEventArgs e)
    {
        Nav.SelectedItem = Nav.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        var page = tag switch
        {
            "dashboard" => typeof(DashboardPage),
            "apps" => typeof(AppsPage),
            "orphans" => typeof(OrphansPage),
            "temp" => typeof(TempPage),
            "dev" => typeof(DevPage),
            "ram" => typeof(RamPage),
            "updater" => typeof(UpdaterPage),
            "filesearch" => typeof(FileSearchPage),
            "services" => typeof(ServicesPage),
            "history" => typeof(HistoryPage),
            "console" => typeof(ConsolePage),
            _ => null
        };
        if (page is not null) ContentFrame.Navigate(page);
    }

    /// <summary>Lets the Dashboard's quick actions jump to a section.</summary>
    public void NavigateTo(string tag)
    {
        foreach (var item in Nav.MenuItems)
            if (item is NavigationViewItem nvi && (nvi.Tag as string) == tag)
            {
                Nav.SelectedItem = nvi;
                return;
            }
    }

    public void ApplyTheme(string theme)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }
}
