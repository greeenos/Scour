using System.Globalization;
using Microsoft.UI.Xaml;
using Scour.Services;

namespace Scour;

/// <summary>
/// Application entry point. The WinUI self-contained, unpackaged bootstrap (configured in the
/// .csproj) generates the actual Main and initializes the Windows App SDK runtime before this runs.
/// </summary>
public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    /// <summary>True when launched at sign-in and configured to start minimized to the tray.</summary>
    public static bool StartedHidden { get; private set; }

    /// <summary>Global, app-lifetime services. Kept simple (no DI container) to mirror the
    /// shared singletons (AppState, FolderSettingsManager) of the macOS original.</summary>
    public static SettingsService Settings { get; } = new SettingsService();

    public static AutoMaintenanceService Maintenance { get; } = new();

    public App()
    {
        // Force English regardless of the machine's locale so dates, number formatting and
        // built-in dialog text stay consistent with the (English) UI strings.
        var en = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = en;
        CultureInfo.DefaultThreadCurrentUICulture = en;
        CultureInfo.CurrentCulture = en;
        CultureInfo.CurrentUICulture = en;
        try { Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en-US"; } catch { }

        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            // Swallow benign teardown exceptions; log everything else to the console manager.
            ConsoleManager.Shared.Append($"Unhandled exception: {e.Message}");
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Settings.Load();

        var cmdline = Environment.GetCommandLineArgs();

        // Headless mode: launched by the scheduled task to run a winget update pass, then exit.
        if (cmdline.Any(a => a.Equals("--run-updates", StringComparison.OrdinalIgnoreCase)))
        {
            _ = RunHeadlessUpdatesAsync();
            return;
        }

        // Start hidden in the tray when auto-launched at sign-in (if minimize-to-tray is on).
        StartedHidden = cmdline.Any(a => a.Equals("--autostart", StringComparison.OrdinalIgnoreCase))
                        && Settings.Current.MinimizeToTray;

        MainWindow = new MainWindow();
        MainWindow.Activate();

        // Background automation (auto RAM trim) while the app is open.
        Maintenance.Start();
    }

    private static async Task RunHeadlessUpdatesAsync()
    {
        try
        {
            await new Services.WingetService().UpdateAllAsync();
            Settings.Current.LastAutoUpdateUtc = DateTime.UtcNow;
            Settings.Save();
        }
        catch { /* nothing visible to surface in headless mode */ }
        finally { Current.Exit(); }
    }
}
