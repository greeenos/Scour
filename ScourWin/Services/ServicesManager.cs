using System.ServiceProcess;
using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Lists Windows services and starts/stops them — the Windows analogue of Scour's
/// Services / launchd daemon manager. Start/stop require elevation.
/// </summary>
public sealed class ServicesManager
{
    public async Task<List<ServiceItem>> ListAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var items = new List<ServiceItem>();
            foreach (var sc in ServiceController.GetServices())
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    items.Add(new ServiceItem
                    {
                        Name = sc.ServiceName,
                        DisplayName = string.IsNullOrEmpty(sc.DisplayName) ? sc.ServiceName : sc.DisplayName,
                        Status = sc.Status.ToString()
                    });
                }
                catch { }
                finally { sc.Dispose(); }
            }
            return items.OrderBy(s => s.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }, ct);
    }

    public async Task<bool> SetRunningAsync(ServiceItem item, bool start)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var sc = new ServiceController(item.Name);
                if (start)
                {
                    if (sc.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
                        sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                }
                else
                {
                    if (sc.CanStop)
                        sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                }
                sc.Refresh();
                item.Status = sc.Status.ToString();
                ConsoleManager.Shared.Append($"{(start ? "Started" : "Stopped")} service {item.Name}.");
                return true;
            }
            catch (Exception ex)
            {
                ConsoleManager.Shared.Append($"Failed to {(start ? "start" : "stop")} {item.Name}: {ex.Message} (try running as administrator)");
                return false;
            }
        });
    }
}
