using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.Models;
using Scour.Services;

namespace Scour.Views;

public sealed partial class SettingsPage : Page
{
    private bool _loading;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        _loading = true;
        var s = App.Settings.Current;
        SensitivityBox.SelectedIndex = (int)s.Sensitivity;
        ThemeBox.SelectedIndex = s.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        RecycleToggle.IsOn = s.SendToRecycleBin;
        ConfirmToggle.IsOn = s.ConfirmBeforeRemoval;
        RegistryToggle.IsOn = s.IncludeRegistry;
        ServicesToggle.IsOn = s.IncludeServices;

        TrayToggle.IsOn = s.MinimizeToTray;
        StartupToggle.IsOn = StartupService.IsEnabled();
        AutoRamToggle.IsOn = s.AutoClearRam;
        AutoUpdateToggle.IsOn = s.AutoUpdateApps;
        RamIntervalBox.Value = s.AutoClearRamMinutes;
        RamThresholdBox.Value = s.AutoClearRamThreshold;

        ExclusionsBox.Text = string.Join(Environment.NewLine, s.AppExclusions);
        _loading = false;
    }

    private void OnAutomationToggle(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var s = App.Settings.Current;

        if (StartupToggle.IsOn != StartupService.IsEnabled())
            StartupService.SetEnabled(StartupToggle.IsOn);

        // Daily updates -> a real Windows Scheduled Task.
        if (AutoUpdateToggle.IsOn && !s.AutoUpdateApps)
            ScheduledTaskService.CreateOrUpdate();
        else if (!AutoUpdateToggle.IsOn && s.AutoUpdateApps)
            ScheduledTaskService.Remove();

        s.MinimizeToTray = TrayToggle.IsOn;
        s.RunOnStartup = StartupToggle.IsOn;
        s.AutoClearRam = AutoRamToggle.IsOn;
        s.AutoUpdateApps = AutoUpdateToggle.IsOn;

        App.Settings.Save();
        App.Maintenance.Reconfigure();
    }

    private void OnNumberChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_loading) return;
        var s = App.Settings.Current;
        if (!double.IsNaN(RamIntervalBox.Value)) s.AutoClearRamMinutes = (int)RamIntervalBox.Value;
        if (!double.IsNaN(RamThresholdBox.Value)) s.AutoClearRamThreshold = (int)RamThresholdBox.Value;
        App.Settings.Save();
        App.Maintenance.Reconfigure();
    }

    private void OnSensitivityChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        App.Settings.Current.Sensitivity = (SearchSensitivity)SensitivityBox.SelectedIndex;
        App.Settings.Save();
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        var theme = ThemeBox.SelectedIndex switch { 1 => "Light", 2 => "Dark", _ => "System" };
        App.Settings.Current.Theme = theme;
        App.Settings.Save();
        (App.MainWindow as MainWindow)?.ApplyTheme(theme);
    }

    private void OnToggle(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var s = App.Settings.Current;
        s.SendToRecycleBin = RecycleToggle.IsOn;
        s.ConfirmBeforeRemoval = ConfirmToggle.IsOn;
        s.IncludeRegistry = RegistryToggle.IsOn;
        s.IncludeServices = ServicesToggle.IsOn;
        App.Settings.Save();
    }

    private void OnExclusionsChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        App.Settings.Current.AppExclusions = ExclusionsBox.Text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
        App.Settings.Save();
    }
}
