namespace Scour.Services;

/// <summary>
/// The set of filesystem roots an app commonly leaves files in — the Windows analogue of
/// Scour's Locations.apps.paths (~/Library, /Library, etc.).
/// </summary>
public static class WindowsLocations
{
    private static string Env(string var) => Environment.GetEnvironmentVariable(var) ?? "";

    public sealed record Root(string Path, bool DeepScan, LeftoverHint Hint);

    public enum LeftoverHint { Generic, StartMenu }

    public static IReadOnlyList<Root> AppRoots(IEnumerable<string> extra)
    {
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localLow = Path.Combine(Directory.GetParent(local)?.FullName ?? local, "LocalLow");
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var startMenuUser = Path.Combine(roaming, @"Microsoft\Windows\Start Menu\Programs");
        var startMenuAll = Path.Combine(programData, @"Microsoft\Windows\Start Menu\Programs");

        var roots = new List<Root>
        {
            // Per-user app data — the richest source of leftovers. Deep scan to reach vendor\app.
            new(roaming, DeepScan: true, LeftoverHint.Generic),
            new(local, DeepScan: true, LeftoverHint.Generic),
            new(Path.Combine(local, "Programs"), DeepScan: true, LeftoverHint.Generic),
            new(localLow, DeepScan: true, LeftoverHint.Generic),
            new(programData, DeepScan: true, LeftoverHint.Generic),
            new(Path.Combine(local, "Packages"), DeepScan: false, LeftoverHint.Generic),
            new(Path.Combine(local, "Temp"), DeepScan: false, LeftoverHint.Generic),
            // Install roots.
            new(Env("ProgramFiles"), DeepScan: false, LeftoverHint.Generic),
            new(Env("ProgramFiles(x86)"), DeepScan: false, LeftoverHint.Generic),
            // Cross-platform/dev tooling configs.
            new(Path.Combine(userProfile, ".config"), DeepScan: true, LeftoverHint.Generic),
            new(userProfile, DeepScan: false, LeftoverHint.Generic),
            // Start Menu shortcuts.
            new(startMenuUser, DeepScan: true, LeftoverHint.StartMenu),
            new(startMenuAll, DeepScan: true, LeftoverHint.StartMenu),
        };

        roots.AddRange(extra.Where(Directory.Exists).Select(p => new Root(p, true, LeftoverHint.Generic)));

        return roots
            .Where(r => !string.IsNullOrEmpty(r.Path) && Directory.Exists(r.Path))
            .GroupBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// Folder names that must never be returned as a removable leftover even if their name
    /// happens to match — they are shared OS/vendor containers. Stored pre-PearFormatted.
    /// </summary>
    public static readonly HashSet<string> ProtectedFolderNames = new(StringComparer.Ordinal)
    {
        "microsoft", "windows", "windowsapps", "commonfiles", "common", "temp", "tmp", "cache",
        "programs", "startup", "packages", "systemtemp", "inetcache", "history", "appdata",
        "local", "locallow", "roaming", "programdata", "users", "public", "default",
        "googleupdate", "windowsdefender", "windowssecurity", "edgeupdate"
    };
}
