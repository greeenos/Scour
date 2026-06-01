using System.Diagnostics;
using System.Text;

namespace Scour.Services;

/// <summary>
/// Manages a real Windows Scheduled Task that runs Scour's headless update pass daily,
/// so app updates happen in the background even when the app isn't open. Uses schtasks.exe;
/// a per-user "run only when logged on" task needs no elevation.
/// </summary>
public static class ScheduledTaskService
{
    public const string TaskName = "Scour\\AutoUpdate";

    private static string ExePath => Environment.ProcessPath ?? "";

    public static bool Exists()
    {
        try
        {
            var (code, _) = Run($"/Query /TN \"{TaskName}\"");
            return code == 0;
        }
        catch { return false; }
    }

    /// <summary>Create or replace the daily task at the given local time (24h, default 12:00).</summary>
    public static bool CreateOrUpdate(string startTime = "12:00")
    {
        if (string.IsNullOrEmpty(ExePath)) return false;
        // /IT = interactive (run only when the user is logged on) so winget has a user context
        // and no stored password is required.
        var args = $"/Create /F /IT /TN \"{TaskName}\" /TR \"\\\"{ExePath}\\\" --run-updates\" /SC DAILY /ST {startTime} /RL LIMITED";
        var (code, output) = Run(args);
        if (code == 0)
            ConsoleManager.Shared.Append($"Scheduled daily auto-update task at {startTime}.");
        else
            ConsoleManager.Shared.Append($"Failed to create scheduled task: {output.Trim()}");
        return code == 0;
    }

    public static void Remove()
    {
        if (!Exists()) return;
        var (code, output) = Run($"/Delete /F /TN \"{TaskName}\"");
        ConsoleManager.Shared.Append(code == 0 ? "Removed scheduled auto-update task." : $"Failed to remove task: {output.Trim()}");
    }

    /// <summary>Trigger the task now (for a manual "test it" action).</summary>
    public static void RunNow()
    {
        if (Exists()) Run($"/Run /TN \"{TaskName}\"");
    }

    private static (int code, string output) Run(string args)
    {
        var psi = new ProcessStartInfo("schtasks.exe", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        using var p = Process.Start(psi)!;
        var sb = new StringBuilder();
        sb.Append(p.StandardOutput.ReadToEnd());
        sb.Append(p.StandardError.ReadToEnd());
        p.WaitForExit(15000);
        return (p.HasExited ? p.ExitCode : -1, sb.ToString());
    }
}
