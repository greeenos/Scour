using CommunityToolkit.Mvvm.ComponentModel;

namespace Scour.Models;

/// <summary>
/// A single installed application discovered from the Windows uninstall registry keys.
/// This is the Windows analogue of macOS's AppInfo (which is built from a .app bundle's Info.plist).
///
/// Identifier mapping macOS -> Windows:
///   bundleIdentifier  -> registry key name / ProductCode GUID (RegistryKeyName)
///   appName           -> DisplayName
///   teamIdentifier    -> Publisher
///   path              -> InstallLocation
/// </summary>
public partial class AppInfo : ObservableObject
{
    /// <summary>Friendly name shown in Programs and Features (DisplayName).</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Publisher / vendor (e.g. "Mozilla").</summary>
    public string Publisher { get; init; } = "";

    /// <summary>Display version string.</summary>
    public string Version { get; init; } = "";

    /// <summary>Install directory on disk, if the entry advertises one.</summary>
    public string InstallLocation { get; init; } = "";

    /// <summary>The QuietUninstallString / UninstallString command, if present.</summary>
    public string UninstallString { get; init; } = "";

    public string QuietUninstallString { get; init; } = "";

    /// <summary>The registry sub-key name under the Uninstall hive (often a ProductCode GUID).</summary>
    public string RegistryKeyName { get; init; } = "";

    /// <summary>Full registry path of the uninstall entry, used for cleanup.</summary>
    public string RegistryKeyPath { get; init; } = "";

    /// <summary>True if the entry lives under HKLM (vs HKCU).</summary>
    public bool IsMachineScope { get; init; }

    /// <summary>True if the entry lives in the 32-bit (WOW6432Node) registry view.</summary>
    public bool IsWow64 { get; init; }

    /// <summary>MSI ProductCode GUID if this is a Windows Installer product.</summary>
    public string? MsiProductCode { get; init; }

    /// <summary>Path to the icon (DisplayIcon), may include ",index".</summary>
    public string IconPath { get; init; } = "";

    /// <summary>Reported install size in bytes (EstimatedSize * 1024), 0 if unknown.</summary>
    public long EstimatedSize { get; init; }

    /// <summary>True for Microsoft Store / UWP packages (no classic uninstall entry).</summary>
    public bool IsStoreApp { get; init; }

    /// <summary>Package full name for Store apps.</summary>
    public string? PackageFullName { get; init; }

    // ----- Scan results (populated by LeftoverScanner) -----

    [ObservableProperty]
    private long _totalLeftoverSize;

    public List<FileItem> Leftovers { get; } = new();

    public override string ToString() => $"{DisplayName} ({Publisher})";
}
