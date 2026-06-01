using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Removes selected leftovers. Files/folders go to the Recycle Bin by default (the Windows
/// analogue of Scour moving items to Trash), registry keys are deleted, services are
/// removed via sc.exe.
/// </summary>
public sealed class RemovalService
{
    private readonly SettingsService.Data _settings;

    public RemovalService(SettingsService.Data settings) => _settings = settings;

    public sealed record Result(int Removed, int Failed, long BytesFreed);

    /// <summary>Run the app's own uninstaller first (best effort), then remove leftovers.</summary>
    public async Task<Result> RemoveAsync(AppInfo app, IEnumerable<FileItem> items, bool runUninstaller, CancellationToken ct = default)
    {
        var list = items.Where(i => i.IsSelected).ToList();
        return await Task.Run(async () =>
        {
            if (runUninstaller)
                await RunUninstaller(app);

            int ok = 0, fail = 0;
            long freed = 0;
            var removedPaths = new List<string>();
            foreach (var item in list)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    Remove(item);
                    ok++;
                    freed += item.Size;
                    removedPaths.Add(item.Path);
                    ConsoleManager.Shared.Append($"Removed: {item.Path}");
                }
                catch (Exception ex)
                {
                    fail++;
                    ConsoleManager.Shared.Append($"Failed to remove {item.Path}: {ex.Message}");
                }
            }
            ConsoleManager.Shared.Append($"Done. Removed {ok}, failed {fail}, freed {Util.FormatBytes(freed)}.");

            if (ok > 0)
            {
                HistoryService.Shared.Add(new HistoryService.Entry
                {
                    AppName = app.DisplayName,
                    ItemCount = ok,
                    BytesFreed = freed,
                    Recycled = _settings.SendToRecycleBin,
                    Paths = removedPaths
                });
            }
            return new Result(ok, fail, freed);
        }, ct);
    }

    private void Remove(FileItem item)
    {
        switch (item.Kind)
        {
            case LeftoverKind.RegistryKey:
                DeleteRegistryKey(item.Path);
                break;
            case LeftoverKind.Service:
                DeleteService(item.Path);
                break;
            default:
                DeleteFileSystem(item.Path);
                break;
        }
    }

    private void DeleteFileSystem(string path)
    {
        var option = _settings.SendToRecycleBin ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently;
        if (Directory.Exists(path))
            FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, option, UICancelOption.DoNothing);
        else if (File.Exists(path))
            FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, option, UICancelOption.DoNothing);
    }

    private static void DeleteRegistryKey(string fullPath)
    {
        // fullPath like "HKCU\SOFTWARE\Vendor\App"
        var idx = fullPath.IndexOf('\\');
        if (idx < 0) return;
        var hivePart = fullPath[..idx];
        var sub = fullPath[(idx + 1)..];

        var (hive, view) = hivePart.ToUpperInvariant() switch
        {
            "HKLM" => (RegistryHive.LocalMachine, sub.Contains("WOW6432Node", StringComparison.OrdinalIgnoreCase) ? RegistryView.Registry32 : RegistryView.Registry64),
            "HKCU" => (RegistryHive.CurrentUser, RegistryView.Registry64),
            "HKCR" => (RegistryHive.ClassesRoot, RegistryView.Registry64),
            _ => (RegistryHive.CurrentUser, RegistryView.Registry64)
        };

        using var baseKey = RegistryKey.OpenBaseKey(hive, view);
        baseKey.DeleteSubKeyTree(sub, throwOnMissingSubKey: false);
    }

    private static void DeleteService(string label)
    {
        var name = label.StartsWith("Service: ") ? label["Service: ".Length..] : label;
        RunProcess("sc.exe", $"delete \"{name}\"");
    }

    private static async Task RunUninstaller(AppInfo app)
    {
        try
        {
            if (app.MsiProductCode is { } code)
            {
                ConsoleManager.Shared.Append($"Running MSI uninstall for {app.DisplayName}...");
                RunProcess("msiexec.exe", $"/x {code} /qb");
            }
            else
            {
                var cmd = !string.IsNullOrEmpty(app.QuietUninstallString) ? app.QuietUninstallString : app.UninstallString;
                if (string.IsNullOrWhiteSpace(cmd)) return;
                ConsoleManager.Shared.Append($"Running uninstaller for {app.DisplayName}...");
                var (file, args) = SplitCommand(cmd);
                RunProcess(file, args);
            }
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            ConsoleManager.Shared.Append($"Uninstaller failed: {ex.Message}");
        }
    }

    private static (string file, string args) SplitCommand(string cmd)
    {
        cmd = cmd.Trim();
        if (cmd.StartsWith("\""))
        {
            var end = cmd.IndexOf('"', 1);
            if (end > 0) return (cmd[1..end], cmd[(end + 1)..].Trim());
        }
        var space = cmd.IndexOf(' ');
        return space < 0 ? (cmd, "") : (cmd[..space], cmd[(space + 1)..].Trim());
    }

    private static void RunProcess(string file, string args)
    {
        var psi = new ProcessStartInfo(file, args) { UseShellExecute = true };
        var p = Process.Start(psi);
        p?.WaitForExit();
    }
}
