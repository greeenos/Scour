using CommunityToolkit.Mvvm.ComponentModel;

namespace Scour.Models;

/// <summary>Kind of leftover so the UI can group/icon them and removal can route correctly.</summary>
public enum LeftoverKind
{
    /// <summary>The primary install directory / the app's own files.</summary>
    InstallDirectory,
    Folder,
    File,
    /// <summary>A registry key (removal deletes the key rather than a filesystem path).</summary>
    RegistryKey,
    /// <summary>A Start Menu shortcut (.lnk).</summary>
    Shortcut,
    /// <summary>A Windows service registered by the app.</summary>
    Service,
    /// <summary>A scheduled task created by the app.</summary>
    ScheduledTask
}

/// <summary>
/// One discovered leftover item (file, folder, or registry key) associated with an app.
/// Windows analogue of the URLs collected by AppPathFinder on macOS.
/// </summary>
public partial class FileItem : ObservableObject
{
    /// <summary>Filesystem path or registry path (e.g. "HKCU\Software\Foo").</summary>
    public string Path { get; init; } = "";

    public LeftoverKind Kind { get; init; }

    /// <summary>Size in bytes (0 for registry keys / services).</summary>
    [ObservableProperty]
    private long _size;

    /// <summary>Whether this item is checked for removal. Defaults true (matches Scour).</summary>
    [ObservableProperty]
    private bool _isSelected = true;

    /// <summary>Display name (last path component).</summary>
    public string Name => Kind == LeftoverKind.RegistryKey || Kind == LeftoverKind.Service || Kind == LeftoverKind.ScheduledTask
        ? Path
        : System.IO.Path.GetFileName(Path.TrimEnd('\\', '/')) is { Length: > 0 } n ? n : Path;

    /// <summary>Human readable size, e.g. "12.4 MB".</summary>
    public string SizeDisplay => Size <= 0 ? "" : Util.FormatBytes(Size);
}
