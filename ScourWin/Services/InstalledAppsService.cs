using Microsoft.Win32;
using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Enumerates installed applications. On macOS Scour scans /Applications for .app bundles;
/// on Windows the equivalent "source of truth" is the Uninstall registry hive
/// (HKLM + HKCU, both 64- and 32-bit views), plus Microsoft Store packages.
/// </summary>
public sealed class InstalledAppsService
{
    private const string UninstallSubKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    public List<AppInfo> GetInstalledApps()
    {
        var byKey = new Dictionary<string, AppInfo>(StringComparer.OrdinalIgnoreCase);

        // HKLM 64-bit, HKLM 32-bit (WOW6432Node), HKCU.
        ReadHive(RegistryHive.LocalMachine, RegistryView.Registry64, machine: true, wow64: false, byKey);
        ReadHive(RegistryHive.LocalMachine, RegistryView.Registry32, machine: true, wow64: true, byKey);
        ReadHive(RegistryHive.CurrentUser, RegistryView.Registry64, machine: false, wow64: false, byKey);

        var apps = byKey.Values.ToList();

        try { apps.AddRange(StoreApps()); }
        catch (Exception ex) { ConsoleManager.Shared.Append($"Store app enumeration skipped: {ex.Message}"); }

        return apps
            .Where(a => !string.IsNullOrWhiteSpace(a.DisplayName))
            .OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static void ReadHive(RegistryHive hive, RegistryView view, bool machine, bool wow64,
        Dictionary<string, AppInfo> sink)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = baseKey.OpenSubKey(UninstallSubKey);
            if (uninstall is null) return;

            foreach (var subName in uninstall.GetSubKeyNames())
            {
                using var k = uninstall.OpenSubKey(subName);
                if (k is null) continue;

                var display = (k.GetValue("DisplayName") as string)?.Trim();
                if (string.IsNullOrEmpty(display)) continue;

                // Skip system components, updates and parent-keyed entries (matches Windows' own
                // Programs & Features filtering).
                if (Convert.ToInt32(k.GetValue("SystemComponent") ?? 0) == 1) continue;
                if (k.GetValue("ParentKeyName") is string pk && pk.Length > 0) continue;
                if (k.GetValue("ReleaseType") is "Security Update" or "Update Rollup" or "Hotfix") continue;

                long size = 0;
                if (k.GetValue("EstimatedSize") is int kb && kb > 0) size = (long)kb * 1024;

                bool isMsi = Guid.TryParse(subName, out _);
                var info = new AppInfo
                {
                    DisplayName = display,
                    Publisher = (k.GetValue("Publisher") as string ?? "").Trim(),
                    Version = (k.GetValue("DisplayVersion") as string ?? "").Trim(),
                    InstallLocation = Util.Expand(k.GetValue("InstallLocation") as string).TrimEnd('\\'),
                    UninstallString = (k.GetValue("UninstallString") as string ?? "").Trim(),
                    QuietUninstallString = (k.GetValue("QuietUninstallString") as string ?? "").Trim(),
                    IconPath = (k.GetValue("DisplayIcon") as string ?? "").Trim(),
                    RegistryKeyName = subName,
                    RegistryKeyPath = $"{(machine ? "HKLM" : "HKCU")}\\{(wow64 ? @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" : UninstallSubKey)}\\{subName}",
                    IsMachineScope = machine,
                    IsWow64 = wow64,
                    MsiProductCode = isMsi ? subName : null,
                    EstimatedSize = size,
                };

                // De-dupe: prefer the entry that advertises an install location.
                var dedupeKey = $"{info.DisplayName}|{info.Version}|{info.Publisher}";
                if (!sink.TryGetValue(dedupeKey, out var existing) ||
                    (string.IsNullOrEmpty(existing.InstallLocation) && !string.IsNullOrEmpty(info.InstallLocation)))
                {
                    sink[dedupeKey] = info;
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleManager.Shared.Append($"Failed reading {hive}/{view}: {ex.Message}");
        }
    }

    /// <summary>Enumerate installed Microsoft Store / UWP packages for the current user.</summary>
    private static IEnumerable<AppInfo> StoreApps()
    {
        var pm = new Windows.Management.Deployment.PackageManager();
        foreach (var pkg in pm.FindPackagesForUser(""))
        {
            if (pkg.IsFramework || pkg.IsResourcePackage || pkg.SignatureKind == Windows.ApplicationModel.PackageSignatureKind.System)
                continue;

            string name;
            string installLocation = "";
            try { name = pkg.DisplayName; } catch { name = pkg.Id.Name; }
            if (string.IsNullOrWhiteSpace(name)) name = pkg.Id.Name;
            try { installLocation = pkg.InstalledLocation?.Path ?? ""; } catch { /* sandboxed */ }

            yield return new AppInfo
            {
                DisplayName = name,
                Publisher = pkg.PublisherDisplayName ?? pkg.Id.Publisher ?? "",
                Version = $"{pkg.Id.Version.Major}.{pkg.Id.Version.Minor}.{pkg.Id.Version.Build}.{pkg.Id.Version.Revision}",
                InstallLocation = installLocation,
                IsStoreApp = true,
                PackageFullName = pkg.Id.FullName,
                RegistryKeyName = pkg.Id.FamilyName,
            };
        }
    }
}
