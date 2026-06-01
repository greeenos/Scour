using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Orphaned-file search — Windows port of ReversePathsFetch. Walks the per-user/app-data roots
/// and flags top-level folders that do NOT correspond to any currently-installed app, i.e.
/// leftovers from software that was already uninstalled.
/// </summary>
public sealed class ReverseScanner
{
    private readonly SettingsService.Data _settings;

    public ReverseScanner(SettingsService.Data settings) => _settings = settings;

    public async Task<List<FileItem>> ScanAsync(IReadOnlyList<AppInfo> installed, CancellationToken ct = default)
        => await Task.Run(() => Scan(installed, ct), ct);

    private List<FileItem> Scan(IReadOnlyList<AppInfo> installed, CancellationToken ct)
    {
        ConsoleManager.Shared.Append("Searching for orphaned files...");

        // Build the corpus of identifiers we consider "claimed" by an installed app.
        var ids = installed.Select(a => new AppIdentifiers(a)).ToList();

        var results = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        foreach (var root in new[] { roaming, local, programData })
        {
            ct.ThrowIfCancellationRequested();
            if (!Directory.Exists(root)) continue;

            IEnumerable<string> dirs;
            try { dirs = Directory.EnumerateDirectories(root); }
            catch { continue; }

            foreach (var dir in dirs)
            {
                ct.ThrowIfCancellationRequested();
                var name = Path.GetFileName(dir);
                var norm = name.PearFormat();
                if (WindowsLocations.ProtectedFolderNames.Contains(norm)) continue;

                bool claimed = ids.Any(id => ClaimsFolder(id, norm));
                if (!claimed)
                {
                    var item = new FileItem { Path = dir, Kind = LeftoverKind.Folder, IsSelected = false };
                    item.Size = SafeSize(dir);
                    results.TryAdd(dir, item);
                }
            }
        }

        ConsoleManager.Shared.Append($"✓ Found {results.Count} orphaned folders ({Util.FormatBytes(results.Values.Sum(i => i.Size))})");
        return results.Values.OrderByDescending(i => i.Size).ToList();
    }

    private static bool ClaimsFolder(AppIdentifiers id, string normalizedFolder)
    {
        if (id.FormattedName.Length >= 3 &&
            (normalizedFolder == id.FormattedName || normalizedFolder.Contains(id.FormattedName) || id.FormattedName.Contains(normalizedFolder)))
            return true;
        if (id.FormattedInstallFolder.Length >= 3 && normalizedFolder.Contains(id.FormattedInstallFolder))
            return true;
        if (id.FormattedPublisher.Length >= 4 && normalizedFolder.Contains(id.FormattedPublisher))
            return true;
        return false;
    }

    private static long SafeSize(string path)
    {
        try { return Directory.Exists(path) ? DirSize(new DirectoryInfo(path)) : 0; }
        catch { return 0; }
    }

    private static long DirSize(DirectoryInfo dir)
    {
        long total = 0;
        try
        {
            foreach (var f in dir.EnumerateFiles()) { try { total += f.Length; } catch { } }
            foreach (var d in dir.EnumerateDirectories())
            {
                try { if ((d.Attributes & FileAttributes.ReparsePoint) != 0) continue; } catch { continue; }
                total += DirSize(d);
            }
        }
        catch { }
        return total;
    }
}
