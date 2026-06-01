using System.Threading;

namespace Scour.Services;

/// <summary>
/// Runs background automation while the app is open. Currently periodic RAM trimming.
/// (Automatic app updates are handled by a real Windows Scheduled Task via
/// <see cref="ScheduledTaskService"/>, so they run even when the app is closed.)
/// </summary>
public sealed class AutoMaintenanceService
{
    private readonly RamCleaner _ram = new();
    private Timer? _ramTimer;
    private int _busy;

    private static SettingsService.Data S => App.Settings.Current;

    public void Start() => Reconfigure();

    /// <summary>(Re)arm the RAM timer to match current settings. Safe to call repeatedly.</summary>
    public void Reconfigure()
    {
        _ramTimer?.Dispose();
        _ramTimer = null;

        if (S.AutoClearRam)
        {
            var period = TimeSpan.FromMinutes(Math.Max(1, S.AutoClearRamMinutes));
            _ramTimer = new Timer(_ => RamTick(), null, period, period);
            ConsoleManager.Shared.Append($"Auto RAM cleaning on: every {S.AutoClearRamMinutes} min above {S.AutoClearRamThreshold}%.");
        }
    }

    private async void RamTick()
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1) return;
        try
        {
            var stats = _ram.GetStats();
            if (stats.LoadPercent >= S.AutoClearRamThreshold)
            {
                ConsoleManager.Shared.Append($"Auto RAM clean triggered ({stats.LoadPercent}% in use)...");
                await _ram.CleanAsync();
            }
        }
        catch (Exception ex) { ConsoleManager.Shared.Append($"Auto RAM clean error: {ex.Message}"); }
        finally { Interlocked.Exchange(ref _busy, 0); }
    }
}
