using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Finds and clears common Windows temporary / cache locations. There's no macOS Scour
/// equivalent — this is a Windows-native addition.
/// </summary>
public sealed class TempCleaner
{
    private static string Env(string v) => Environment.GetEnvironmentVariable(v) ?? "";

    public List<CleanupCategory> BuildCategories()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var windir = Env("SystemRoot");

        var cats = new List<CleanupCategory>
        {
            new() { Name = "User temp files", Description = "%TEMP% — per-user scratch files", Paths = { Env("TEMP"), Path.Combine(local, "Temp") } },
            new() { Name = "Windows temp files", Description = @"C:\Windows\Temp (needs admin)", Paths = { Path.Combine(windir, "Temp") } },
            new() { Name = "Windows Update cache", Description = "Downloaded update packages (needs admin)", Paths = { Path.Combine(windir, "SoftwareDistribution", "Download") } },
            new() { Name = "Thumbnail & icon cache", Description = "Explorer thumbnail/icon database", Paths = { Path.Combine(local, "Microsoft", "Windows", "Explorer") } },
            new() { Name = "Internet Explorer / WinINET cache", Description = "Legacy web cache", Paths = { Path.Combine(local, "Microsoft", "Windows", "INetCache") } },
            new() { Name = "Crash dumps", Description = "Application crash dump files", Paths = { Path.Combine(local, "CrashDumps") } },
            new() { Name = "Delivery Optimization", Description = "P2P update cache (needs admin)", Paths = { Path.Combine(windir, "SoftwareDistribution", "DeliveryOptimization") } },
            new() { Name = "Recent items", Description = "Recently-opened file shortcuts", Paths = { Path.Combine(Env("APPDATA"), "Microsoft", "Windows", "Recent") } },
        };

        return cats.Where(c => c.ExistingPaths.Any()).ToList();
    }

    public async Task SizeAsync(IEnumerable<CleanupCategory> cats, CancellationToken ct = default)
    {
        foreach (var c in cats)
        {
            ct.ThrowIfCancellationRequested();
            c.IsScanning = true;
            var paths = c.ExistingPaths.ToList();
            c.Size = await Task.Run(() => paths.Sum(FsUtil.SizeOf), ct);
            c.IsScanning = false;
        }
    }

    public async Task<(long freed, int skipped)> CleanAsync(IEnumerable<CleanupCategory> cats, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            long freed = 0; int skipped = 0;
            foreach (var c in cats.Where(c => c.IsSelected))
            {
                ct.ThrowIfCancellationRequested();
                ConsoleManager.Shared.Append($"Cleaning {c.Name}...");
                foreach (var p in c.ExistingPaths)
                {
                    var (f, s) = FsUtil.ClearContents(p, c.RemoveRootToo);
                    freed += f; skipped += s;
                }
                c.Size = c.ExistingPaths.Sum(FsUtil.SizeOf);
            }
            ConsoleManager.Shared.Append($"Temp cleanup freed {Util.FormatBytes(freed)} ({skipped} item(s) skipped/in use).");
            return (freed, skipped);
        }, ct);
    }
}
