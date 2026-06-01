using Microsoft.Win32;
using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Finds all files, folders, registry keys, and services associated with an app.
/// Windows port of AppPathFinder: build normalized identifiers, walk known locations,
/// match item names, then de-duplicate nested paths and size everything.
/// </summary>
public sealed class LeftoverScanner
{
    private readonly SettingsService.Data _settings;

    public LeftoverScanner(SettingsService.Data settings) => _settings = settings;

    public async Task<List<FileItem>> ScanAsync(AppInfo app, CancellationToken ct = default)
    {
        return await Task.Run(() => Scan(app, ct), ct);
    }

    private List<FileItem> Scan(AppInfo app, CancellationToken ct)
    {
        var console = ConsoleManager.Shared;
        console.Append($"Searching for files related to {app.DisplayName}...");

        var id = new AppIdentifiers(app);
        var found = new Dictionary<string, FileItem>(StringComparer.OrdinalIgnoreCase);

        void AddPath(string path, LeftoverKind kind)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (IsExcluded(path)) return;
            found.TryAdd(path, new FileItem { Path = path, Kind = kind });
        }

        // 1. The install directory itself (analogue of initialURLProcessing).
        if (!string.IsNullOrEmpty(app.InstallLocation) && Directory.Exists(app.InstallLocation))
            AddPath(app.InstallLocation, LeftoverKind.InstallDirectory);

        // 2. Walk filesystem roots.
        var roots = WindowsLocations.AppRoots(_settings.ExtraSearchRoots);
        console.Append($"Scanning {roots.Count} locations...");
        foreach (var root in roots)
        {
            ct.ThrowIfCancellationRequested();
            ScanRoot(root, id, app, found, AddPath);
        }

        // 3. Registry keys under Software hives.
        if (_settings.IncludeRegistry)
            ScanRegistry(id, app, AddPath, ct);

        // 4. Services (registry-based, read-only).
        if (_settings.IncludeServices)
            ScanServices(id, app, AddPath, ct);

        // 5. The uninstall registry key itself.
        if (_settings.IncludeRegistry && !string.IsNullOrEmpty(app.RegistryKeyPath))
            AddPath(app.RegistryKeyPath, LeftoverKind.RegistryKey);

        // De-duplicate nested filesystem paths (keep the highest ancestor), matching
        // finalizeCollection's child-path removal.
        var deduped = DedupeNested(found.Values);

        console.Append($"Calculating sizes for {deduped.Count} items...");
        foreach (var item in deduped)
        {
            ct.ThrowIfCancellationRequested();
            if (item.Kind is LeftoverKind.RegistryKey or LeftoverKind.Service) continue;
            item.Size = SafeSize(item.Path);
        }

        var total = deduped.Sum(i => i.Size);
        console.Append($"✓ Found {deduped.Count} items ({Util.FormatBytes(total)})");
        return deduped;
    }

    private void ScanRoot(WindowsLocations.Root root, AppIdentifiers id, AppInfo app,
        Dictionary<string, FileItem> found, Action<string, LeftoverKind> add)
    {
        var startMenu = root.Hint == WindowsLocations.LeftoverHint.StartMenu;
        try
        {
            foreach (var entry in EnumerateEntries(root.Path))
            {
                var name = NameOf(entry);
                var normalized = NameForMatch(entry, name);

                bool isDir = Directory.Exists(entry);

                if (NameMatcher.Matches(normalized, id, _settings.Sensitivity) &&
                    !IsProtectedTopLevel(entry, root.Path, normalized))
                {
                    add(entry, ClassifyKind(entry, isDir, startMenu));
                    continue; // matched: don't descend further into it
                }

                // Descend one level for deep-scan roots so we reach vendor\app layouts,
                // e.g. %AppData%\Mozilla\Firefox.
                if (root.DeepScan && isDir)
                {
                    bool publisherFolder = id.FormattedPublisher.Length >= AppIdentifiers.MinContainsLen &&
                                           normalized.Contains(id.FormattedPublisher);
                    foreach (var child in EnumerateEntries(entry))
                    {
                        var cName = NameOf(child);
                        var cNorm = NameForMatch(child, cName);
                        if (NameMatcher.Matches(cNorm, id, _settings.Sensitivity))
                        {
                            // If the parent is a dedicated publisher folder, prefer removing the
                            // whole vendor folder (analogue of Scour's vendor-folder logic).
                            var target = publisherFolder ? entry : child;
                            add(target, ClassifyKind(target, Directory.Exists(target), startMenu));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Shared.Append($"Skipped {root.Path}: {ex.Message}");
        }
    }

    private void ScanRegistry(AppIdentifiers id, AppInfo app, Action<string, LeftoverKind> add, CancellationToken ct)
    {
        (RegistryHive hive, RegistryView view, string label, string sub)[] targets =
        {
            (RegistryHive.CurrentUser, RegistryView.Registry64, "HKCU", "SOFTWARE"),
            (RegistryHive.LocalMachine, RegistryView.Registry64, "HKLM", "SOFTWARE"),
            (RegistryHive.LocalMachine, RegistryView.Registry32, "HKLM", @"SOFTWARE\WOW6432Node"),
        };

        foreach (var (hive, view, label, sub) in targets)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var software = baseKey.OpenSubKey(sub);
                if (software is null) continue;

                foreach (var vendor in software.GetSubKeyNames())
                {
                    var vNorm = vendor.PearFormat();
                    if (NameMatcher.Matches(vNorm, id, _settings.Sensitivity) &&
                        !WindowsLocations.ProtectedFolderNames.Contains(vNorm))
                    {
                        add($@"{label}\{sub}\{vendor}", LeftoverKind.RegistryKey);
                        continue;
                    }

                    // Publisher\App style: descend one level under a matching publisher key.
                    if (id.FormattedPublisher.Length >= AppIdentifiers.MinContainsLen &&
                        vNorm.Contains(id.FormattedPublisher) &&
                        !WindowsLocations.ProtectedFolderNames.Contains(vNorm))
                    {
                        try
                        {
                            using var vendorKey = software.OpenSubKey(vendor);
                            foreach (var appKey in vendorKey?.GetSubKeyNames() ?? Array.Empty<string>())
                                if (NameMatcher.Matches(appKey.PearFormat(), id, _settings.Sensitivity))
                                    add($@"{label}\{sub}\{vendor}\{appKey}", LeftoverKind.RegistryKey);
                        }
                        catch { /* access denied on a subkey */ }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.Shared.Append($"Registry scan skipped {label}\\{sub}: {ex.Message}");
            }
        }
    }

    private void ScanServices(AppIdentifiers id, AppInfo app, Action<string, LeftoverKind> add, CancellationToken ct)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var services = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (services is null) return;

            var installLoc = app.InstallLocation.PearFormat();
            foreach (var svc in services.GetSubKeyNames())
            {
                ct.ThrowIfCancellationRequested();
                using var k = services.OpenSubKey(svc);
                if (k is null) continue;

                var image = (k.GetValue("ImagePath") as string ?? "");
                var imageNorm = image.PearFormat();
                var svcNorm = svc.PearFormat();

                bool match = NameMatcher.Matches(svcNorm, id, _settings.Sensitivity);
                if (!match && installLoc.Length > 8 && imageNorm.Contains(installLoc))
                    match = true;

                if (match)
                    add($"Service: {svc}", LeftoverKind.Service);
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Shared.Append($"Service scan skipped: {ex.Message}");
        }
    }

    // ----- helpers -----

    private static IEnumerable<string> EnumerateEntries(string dir)
    {
        IEnumerable<string> items;
        try { items = Directory.EnumerateFileSystemEntries(dir); }
        catch { yield break; }
        foreach (var i in items) yield return i;
    }

    private static string NameOf(string path) => Path.GetFileName(path.TrimEnd('\\', '/'));

    /// <summary>Normalize a name for matching; strip the extension for files (matches Swift).</summary>
    private static string NameForMatch(string fullPath, string name)
    {
        bool isDir = Directory.Exists(fullPath);
        if (!isDir && Path.HasExtension(name))
            name = Path.GetFileNameWithoutExtension(name);
        return name.PearFormat();
    }

    private static LeftoverKind ClassifyKind(string path, bool isDir, bool startMenu)
    {
        if (startMenu && path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) return LeftoverKind.Shortcut;
        return isDir ? LeftoverKind.Folder : LeftoverKind.File;
    }

    /// <summary>Don't ever return a scan root itself or a shared OS container as a leftover.</summary>
    private static bool IsProtectedTopLevel(string path, string rootPath, string normalizedName)
    {
        if (string.Equals(path.TrimEnd('\\'), rootPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            return true;
        return WindowsLocations.ProtectedFolderNames.Contains(normalizedName);
    }

    private bool IsExcluded(string path)
    {
        var norm = path.PearFormat();
        foreach (var ex in _settings.AppExclusions)
        {
            var e = ex.PearFormat();
            if (e.Length == 0) continue;
            if (norm == e || norm.Contains(e)) return true;
        }
        return false;
    }

    private static List<FileItem> DedupeNested(IEnumerable<FileItem> items)
    {
        var fsItems = items
            .Where(i => i.Kind is not (LeftoverKind.RegistryKey or LeftoverKind.Service))
            .OrderBy(i => i.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var others = items.Where(i => i.Kind is LeftoverKind.RegistryKey or LeftoverKind.Service).ToList();

        var kept = new List<FileItem>();
        foreach (var item in fsItems)
        {
            var p = item.Path.TrimEnd('\\') + "\\";
            if (kept.Any(k => p.StartsWith(k.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)))
                continue;
            kept.Add(item);
        }
        kept.AddRange(others);
        return kept;
    }

    private static long SafeSize(string path)
    {
        try
        {
            if (File.Exists(path)) return new FileInfo(path).Length;
            if (Directory.Exists(path)) return DirSize(new DirectoryInfo(path));
        }
        catch { /* access denied / locked */ }
        return 0;
    }

    private static long DirSize(DirectoryInfo dir)
    {
        long total = 0;
        try
        {
            foreach (var f in dir.EnumerateFiles())
            {
                try { total += f.Length; } catch { }
            }
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
