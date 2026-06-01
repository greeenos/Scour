using Microsoft.Win32;

namespace Scour.Services;

/// <summary>
/// Manages "run at sign-in" by writing the app path to the per-user Run key
/// (HKCU\...\CurrentVersion\Run). No elevation needed.
/// </summary>
public static class StartupService
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Scour";

    private static string ExePath => Environment.ProcessPath ?? "";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string s && s.Trim('"').Equals(ExePath, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (key is null) return;
            if (enabled)
            {
                key.SetValue(ValueName, $"\"{ExePath}\" --autostart");
                ConsoleManager.Shared.Append("Enabled run at startup.");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                ConsoleManager.Shared.Append("Disabled run at startup.");
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Shared.Append($"Failed to update startup setting: {ex.Message}");
        }
    }
}
